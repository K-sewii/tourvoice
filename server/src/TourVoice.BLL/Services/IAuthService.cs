using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
}

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException() : base("Email da duoc su dung.") { }
}