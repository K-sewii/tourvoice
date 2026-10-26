using Microsoft.Extensions.Options;
using Moq;
using TourVoice.BLL.Auth;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class AuthServiceTests
{
    private static IOptions<JwtSettings> MakeJwtSettings() => Options.Create(new JwtSettings
    {
        Key = "unit-test-secret-key-khong-duoc-lo-ra-ngoai-32ky-tu",
        Issuer = "TourVoiceTest",
        Audience = "TourVoiceTestClient",
        ExpiryMinutes = 60
    });

    [Fact]
    public async Task RegisterAsync_KhiEmailChuaTonTai_TraVeTokenHopLe()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.EmailExistsAsync("new@test.com")).ReturnsAsync(false);

        var service = new AuthService(repo.Object, MakeJwtSettings());
        var result = await service.RegisterAsync(new RegisterRequestDto("new@test.com", "Pass@123", "Nguoi Dung"));

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("new@test.com", result.Email);
        Assert.Equal("Customer", result.Role);
        repo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_KhiEmailDaTonTai_NemLoi()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.EmailExistsAsync("trung@test.com")).ReturnsAsync(true);

        var service = new AuthService(repo.Object, MakeJwtSettings());

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            service.RegisterAsync(new RegisterRequestDto("trung@test.com", "Pass@123", "A")));
    }

    [Fact]
    public async Task LoginAsync_SaiMatKhau_TraVeNull()
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user = new User { Id = 1, Email = "a@test.com", FullName = "A", Role = UserRole.Customer, IsActive = true };
        user.PasswordHash = hasher.HashPassword(user, "MatKhauDung");

        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetUserByEmailAsync("a@test.com")).ReturnsAsync(user);

        var service = new AuthService(repo.Object, MakeJwtSettings());
        var result = await service.LoginAsync(new LoginRequestDto("a@test.com", "MatKhauSai"));

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_DungMatKhau_TraVeToken()
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user = new User { Id = 1, Email = "a@test.com", FullName = "A", Role = UserRole.Customer, IsActive = true };
        user.PasswordHash = hasher.HashPassword(user, "MatKhauDung");

        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetUserByEmailAsync("a@test.com")).ReturnsAsync(user);

        var service = new AuthService(repo.Object, MakeJwtSettings());
        var result = await service.LoginAsync(new LoginRequestDto("a@test.com", "MatKhauDung"));

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Token));
    }
}