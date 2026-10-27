namespace TourVoice.DAL.Entities;

public class Place
{
    public int Id { get; set; }
    public string Slug { get; set; } = null!;          // duy nhất, khoa sau Publish lan dau
    public PlaceKind Kind { get; set; }
    public string? Category { get; set; }                // Heritage, Market, FoodStreet, Park
    public string? FoodType { get; set; }                 // Noodles, Rice, Snack, Drink, Dessert
    public int? ParentId { get; set; }                    // mon -> diem den cha
    public Place? Parent { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? CoverImageUrl { get; set; }
    public PlaceStatus Status { get; set; } = PlaceStatus.Draft;
    public int SortOrder { get; set; }
    public string? Address { get; set; }
    public string? PriceRange { get; set; }
    public string? OpeningHours { get; set; }

    public ICollection<Place> Children { get; set; } = new List<Place>();
    public ICollection<PlaceTranslation> Translations { get; set; } = new List<PlaceTranslation>();
}