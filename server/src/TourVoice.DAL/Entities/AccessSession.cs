namespace TourVoice.DAL.Entities;

public class AccessSession
{
    public long Id { get; set; }
    public string DeviceKey { get; set; } = null!;         // SHA-256, khong luu deviceId goc
    public AccessSource Source { get; set; }
    public int? EntryPlaceId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}