using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface IPlaceRepository
{
    Task<List<Place>> GetPublishedDestinationsAsync();
    Task<Place?> GetPublishedBySlugAsync(string slug);
}