using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface IPlaceRepository
{
    Task<List<Place>> GetPublishedDestinationsAsync();
    Task<Place?> GetPublishedBySlugAsync(string slug);

    // Khong AsNoTracking - dung cho admin sua truc tiep roi SaveChangesAsync
    Task<Place?> GetByIdAsync(int id);
    Task SaveChangesAsync();
}