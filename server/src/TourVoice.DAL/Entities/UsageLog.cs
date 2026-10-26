namespace TourVoice.DAL.Entities;

public class UsageLog
{
    public long Id { get; set; }
    public UsageService Service { get; set; }
    public int Characters { get; set; }
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}