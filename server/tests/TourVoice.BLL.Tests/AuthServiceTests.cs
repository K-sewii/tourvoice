using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using TourVoice.BLL.Auth;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
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
        CustomerExpiryDays = 7,
        AdminExpiryHours = 8,
    });

    private static User MakeUser(string password, UserRole role = UserRole.Customer, bool active = true)
    {
        var user = new User { Id = 1, Email = "a@test.com", FullName = "A", Role = role, IsActive = active };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        return user;
    }

    private static DateTime ExpiryOf(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo;

    [Fact]
    public async Task RegisterAsync_KhiEmailChuaTonTai_ChuanHoaEmailVaTraToken()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.EmailExistsAsync("new@test.com")).ReturnsAsync(false);

        var service = new AuthService(repo.Object, MakeJwtSettings());
        var result = await service.RegisterAsync(new RegisterRequestDto("  New@Test.COM ", "Pass@123", " Nguoi Dung "));

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("new@test.com", result.Email);
        Assert.Equal("Nguoi Dung", result.FullName);
        Assert.Equal("Customer", result.Role);
        repo.Verify(r => r.AddUserAsync(It.Is<User>(u => u.Email == "new@test.com")), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_KhiEmailDaTonTai_NemConflict()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.EmailExistsAsync("trung@test.com")).ReturnsAsync(true);

        var service = new AuthService(repo.Object, MakeJwtSettings());

        var ex = await Assert.ThrowsAsync<EmailAlreadyExistsException>(() =>
            service.RegisterAsync(new RegisterRequestDto("TRUNG@test.com", "Pass@123", "A")));
        Assert.Equal("email_taken", ex.Code);
    }

    [Theory]
    [InlineData("khong-phai-email", "Pass@123", "A", "invalid_email")]
    [InlineData("a@test.com", "1234567", "A", "weak_password")]   // BR-12: >= 8 ky tu
    [InlineData("a@test.com", "Pass@123", "   ", "invalid_full_name")]
    public async Task RegisterAsync_DuLieuKhongHopLe_NemValidation(string email, string password, string name, string code)
    {
        var repo = new Mock<ICustomerRepository>();
        var service = new AuthService(repo.Object, MakeJwtSettings());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegisterAsync(new RegisterRequestDto(email, password, name)));

        Assert.Equal(code, ex.Code);
        repo.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_SaiMatKhau_Nem401()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetUserByEmailAsync("a@test.com")).ReturnsAsync(MakeUser("MatKhauDung"));

        var service = new AuthService(repo.Object, MakeJwtSettings());

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new LoginRequestDto("a@test.com", "MatKhauSai")));
        Assert.Equal("invalid_credentials", ex.Code);
    }

    [Fact]
    public async Task LoginAsync_SaiEmail_CungLoi401NhuSaiMatKhau()
    {
        var repo = new Mock<ICustomerRepository>();
        var service = new AuthService(repo.Object, MakeJwtSettings());

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new LoginRequestDto("khongco@test.com", "gi-cung-duoc")));
        Assert.Equal("invalid_credentials", ex.Code);
    }

    [Fact]
    public async Task LoginAsync_TaiKhoanKhoa_Nem403SauKhiDungMatKhau()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetUserByEmailAsync("a@test.com")).ReturnsAsync(MakeUser("MatKhauDung", active: false));
        var service = new AuthService(repo.Object, MakeJwtSettings());

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.LoginAsync(new LoginRequestDto("a@test.com", "MatKhauDung")));
        Assert.Equal("account_locked", ex.Code);
    }

    [Fact]
    public async Task LoginAsync_Customer_TokenHan7Ngay_EmailKhongPhanBietHoaThuong()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetUserByEmailAsync("a@test.com")).ReturnsAsync(MakeUser("MatKhauDung"));
        var service = new AuthService(repo.Object, MakeJwtSettings());

        var result = await service.LoginAsync(new LoginRequestDto(" A@Test.com", "MatKhauDung"));

        Assert.InRange(ExpiryOf(result.Token) - DateTime.UtcNow, TimeSpan.FromDays(6.9), TimeSpan.FromDays(7.1));
    }

    [Fact]
    public async Task LoginAsync_Admin_TokenHan8Gio()
    {
        var repo = new Mock<ICustomerRepository>();
        repo.Setup(r => r.GetUserByEmailAsync("a@test.com")).ReturnsAsync(MakeUser("MatKhauDung", UserRole.Admin));
        var service = new AuthService(repo.Object, MakeJwtSettings());

        var result = await service.LoginAsync(new LoginRequestDto("a@test.com", "MatKhauDung"));

        Assert.Equal("Admin", result.Role);
        Assert.InRange(ExpiryOf(result.Token) - DateTime.UtcNow, TimeSpan.FromHours(7.9), TimeSpan.FromHours(8.1));
    }
}
