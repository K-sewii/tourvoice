namespace TourVoice.DAL.Entities;

public class Favorite
{
    public int UserId { get; set; }
    public int PlaceId { get; set; }
    public Place Place { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
