namespace TourVoice.BLL.Dtos;

public record AdminUserDto(int Id, string Email, string FullName, string Role, bool IsActive);

public record CreateAdminRequestDto(string Email, string Password, string FullName);
