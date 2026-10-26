namespace TourVoice.DAL.Entities;

public class Visit
{
    public int UserId { get; set; }
    public int PlaceId { get; set; }
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
}