using Microsoft.AspNetCore.Identity;
using Moq;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class AdminUserServiceTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly AdminUserService _service;

    public AdminUserServiceTests() => _service = new AdminUserService(_repo.Object);

    private static User UserOf(int id, UserRole role = UserRole.Customer, bool active = true) => new()
    {
        Id = id, Email = $"u{id}@x.vn", FullName = $"User {id}", Role = role, IsActive = active,
    };

    // ---- list ----

    [Fact]
    public async Task GetList_NormalizesSearch_ParsesRole_AndClampsPaging()
    {
        _repo.Setup(r => r.GetPageAsync("an", UserRole.Admin, 1, 100)).ReturnsAsync(([UserOf(1, UserRole.Admin)], 1));

        var result = await _service.GetListAsync("  AN ", "admin", 0, 5000);

        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal("Admin", result.Items.Single().Role);
    }

    [Fact]
    public async Task GetList_RejectsUnknownRole()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.GetListAsync(null, "root", 1, 20));
        Assert.Equal("invalid_role", ex.Code);
    }

    // ---- create admin ----

    [Fact]
    public async Task CreateAdmin_HashesPassword_AndStoresLowercaseEmail()
    {
        User? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<User>())).Callback<User>(u => saved = u).Returns(Task.CompletedTask);

        var dto = await _service.CreateAdminAsync(new(" New.Admin@X.vn ", "longenough1", " Admin Mới "));

        Assert.Equal("Admin", dto.Role);
        Assert.Equal("new.admin@x.vn", saved!.Email);
        Assert.Equal("Admin Mới", saved.FullName);
        Assert.NotEqual("longenough1", saved.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, "longenough1"));
    }

    [Theory]
    [InlineData("not-an-email", "longenough1", "A", "invalid_email")]
    [InlineData("a@x.vn", "short", "A", "weak_password")]
    [InlineData("a@x.vn", "longenough1", "  ", "invalid_full_name")]
    public async Task CreateAdmin_Validates(string email, string password, string name, string code)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAdminAsync(new(email, password, name)));
        Assert.Equal(code, ex.Code);
        _repo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAdmin_Throws_WhenEmailTaken()
    {
        _repo.Setup(r => r.EmailExistsAsync("a@x.vn")).ReturnsAsync(true);
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => _service.CreateAdminAsync(new("A@x.vn", "longenough1", "A")));
    }

    // ---- lock / unlock (BR-11) ----

    [Fact]
    public async Task SetActive_LocksCustomer()
    {
        var user = UserOf(2);
        _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);

        var dto = await _service.SetActiveAsync(2, false, actingUserId: 1);

        Assert.False(dto!.IsActive);
        Assert.False(user.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetActive_ReturnsNull_WhenUserMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((User?)null);
        Assert.Null(await _service.SetActiveAsync(9, false, 1));
    }

    [Fact]
    public async Task SetActive_RefusesToLockLastActiveAdmin_BR11()
    {
        var admin = UserOf(2, UserRole.Admin);
        _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(admin);
        _repo.Setup(r => r.CountActiveAdminsAsync()).ReturnsAsync(1);

        await Assert.ThrowsAsync<LastAdminException>(() => _service.SetActiveAsync(2, false, actingUserId: 1));

        Assert.True(admin.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetActive_LocksAdmin_WhenAnotherActiveAdminRemains()
    {
        var admin = UserOf(2, UserRole.Admin);
        _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(admin);
        _repo.Setup(r => r.CountActiveAdminsAsync()).ReturnsAsync(2);

        await _service.SetActiveAsync(2, false, actingUserId: 1);

        Assert.False(admin.IsActive);
    }

    [Fact]
    public async Task SetActive_RefusesSelfLock()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(UserOf(1, UserRole.Admin));
        _repo.Setup(r => r.CountActiveAdminsAsync()).ReturnsAsync(5);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.SetActiveAsync(1, false, actingUserId: 1));
        Assert.Equal("cannot_lock_self", ex.Code);
    }

    [Fact]
    public async Task SetActive_Unlocks_AndIsIdempotent()
    {
        var locked = UserOf(2, active: false);
        _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(locked);

        await _service.SetActiveAsync(2, true, 1);
        await _service.SetActiveAsync(2, true, 1);   // lan hai khong doi gi

        Assert.True(locked.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
