namespace TourVoice.DAL.Entities;

public class UsageLog
{
    public long Id { get; set; }
    public UsageService Service { get; set; } = UsageService.Translator;
    public int Characters { get; set; }
    public int Chunks { get; set; }
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}