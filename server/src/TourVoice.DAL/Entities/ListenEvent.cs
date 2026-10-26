namespace TourVoice.DAL.Entities;

public class ListenEvent
{
    public long Id { get; set; }
    public int PlaceId { get; set; }
    public string LanguageCode { get; set; } = null!;
    public int? UserId { get; set; }                       // null = khach vang lai
    public int ListenedSeconds { get; set; }
    public ListenSource Source { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}