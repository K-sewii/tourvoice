namespace TourVoice.DAL.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;             // duy nhat
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Customer;
    public string? PreferredLanguageCode { get; set; }
    public bool IsActive { get; set; } = true;
}