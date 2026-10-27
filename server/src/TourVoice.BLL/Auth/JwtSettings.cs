namespace TourVoice.BLL.Auth;

/// <summary>JWT HS256 (ADR-06): khoa >= 32 byte; han 7 ngay cho Customer, 8 gio cho Admin; khong refresh token.</summary>
public class JwtSettings
{
    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int CustomerExpiryDays { get; set; } = 7;
    public int AdminExpiryHours { get; set; } = 8;
}
