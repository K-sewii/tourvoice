using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IAuthService
{
    /// <exception cref="Exceptions.ValidationException">Email/mat khau/ho ten khong hop le (BR-12).</exception>
    /// <exception cref="Exceptions.ConflictException">Email da ton tai.</exception>
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

    /// <exception cref="Exceptions.UnauthorizedException">Sai email hoac mat khau (cung mot loi, BR-12).</exception>
    /// <exception cref="Exceptions.ForbiddenException">Tai khoan bi khoa (BR-11).</exception>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
}
