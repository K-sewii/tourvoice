namespace TourVoice.BLL.Dtos;

public record RegisterRequestDto(string Email, string Password, string FullName);
public record LoginRequestDto(string Email, string Password);
public record AuthResponseDto(string Token, string Email, string FullName, string Role);
public record UserProfileDto(int Id, string Email, string FullName, string Role, string? PreferredLanguageCode);