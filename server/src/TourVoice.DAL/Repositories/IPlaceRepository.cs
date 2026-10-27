using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface IPlaceRepository
{
    /// <summary>Diem den Published (khong cha), kem ban dich, theo SortOrder.</summary>
    Task<List<Place>> GetPublishedDestinationsAsync();

    /// <summary>
    /// Moi Place khach thay duoc: diem den Published va mon Published co cha Published.
    /// Kem ban dich cua chinh no va cua cha (mot truy van, tranh N+1).
    /// </summary>
    Task<List<Place>> GetPublishedCatalogAsync();

    /// <summary>
    /// Place Published theo slug (mon thi cha cung phai Published), kem ban dich,
    /// cha + ban dich cua cha, va cac mon con Published.
    /// </summary>
    Task<Place?> GetPublishedBySlugAsync(string slug);

    Task<int?> GetPublishedIdBySlugAsync(string slug);
    Task<bool> IsPublishedAsync(int placeId);
    Task<int> CountPublishedDestinationsAsync();

    // Khong AsNoTracking - dung cho admin sua truc tiep roi SaveChangesAsync
    Task<Place?> GetByIdAsync(int id);
    Task SaveChangesAsync();

    /// <summary>
    /// Cay admin (SCR-A3): trang cac diem den (moi Status), kem ban dich va mon con + ban dich.
    /// <paramref name="status"/> != null: giu diem den neu chinh no hoac mot mon con co trang thai do.
    /// </summary>
    Task<(List<Place> Items, int Total)> GetAdminTreeAsync(PlaceStatus? status, int page, int pageSize);

    /// <summary>Chi tiet cho Place editor: ban dich, cha (+ ban dich), mon con (+ ban dich). AsNoTracking.</summary>
    Task<Place?> GetAdminDetailAsync(int id);

    /// <summary>Moi Place kem ban dich cho ma tran thuyet minh (SCR-A5), diem den truoc roi mon con.</summary>
    Task<List<Place>> GetAllWithTranslationsAsync();

    Task<bool> SlugExistsAsync(string slug);
    Task<bool> HasChildrenAsync(int id);
    Task AddAsync(Place place);
    Task RemoveAsync(Place place);
}
