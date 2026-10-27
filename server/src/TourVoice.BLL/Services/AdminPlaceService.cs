using TourVoice.BLL.Dtos;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class AdminPlaceService : IAdminPlaceService
{
    private readonly IPlaceRepository _repository;
    public AdminPlaceService(IPlaceRepository repository) => _repository = repository;

    public async Task<PagedResultDto<AdminPlaceListItemDto>> GetListAsync(int page, int pageSize)
    {
        var (items, total) = await _repository.GetAdminListAsync(page, pageSize);
        var dtos = items.Select(p => new AdminPlaceListItemDto(
            p.Id, p.Slug, p.Category, p.Status.ToString(), p.SortOrder,
            p.Translations.Any(t => t.LanguageCode == "vi" && !string.IsNullOrWhiteSpace(t.NarrationText)))).ToList();
        return new PagedResultDto<AdminPlaceListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<AdminPlaceDetailDto?> GetByIdAsync(int id)
    {
        var p = await _repository.GetByIdAsync(id);
        return p is null ? null : ToDetailDto(p);
    }

    public async Task<AdminPlaceDetailDto> CreateAsync(CreatePlaceRequestDto request)
    {
        if (await _repository.SlugExistsAsync(request.Slug))
            throw new SlugAlreadyExistsException();

        var place = new Place
        {
            Slug = request.Slug,
            Kind = Enum.Parse<PlaceKind>(request.Kind, ignoreCase: true),
            Category = request.Category,
            FoodType = request.FoodType,
            ParentId = request.ParentId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SortOrder = request.SortOrder,
            Address = request.Address,
            PriceRange = request.PriceRange,
            OpeningHours = request.OpeningHours,
            Status = PlaceStatus.Draft
        };
        await _repository.AddAsync(place);
        return ToDetailDto(place);
    }

    public async Task<AdminPlaceDetailDto?> UpdateAsync(int id, UpdatePlaceRequestDto request)
    {
        var place = await _repository.GetByIdAsync(id);
        if (place is null) return null;

        place.Category = request.Category;
        place.FoodType = request.FoodType;
        place.Latitude = request.Latitude;
        place.Longitude = request.Longitude;
        place.CoverImageUrl = request.CoverImageUrl;
        place.SortOrder = request.SortOrder;
        place.Address = request.Address;
        place.PriceRange = request.PriceRange;
        place.OpeningHours = request.OpeningHours;

        await _repository.SaveChangesAsync();
        return ToDetailDto(place);
    }

    public async Task<bool> PublishAsync(int id)
    {
        var place = await _repository.GetByIdAsync(id);
        if (place is null) return false;
        place.Status = PlaceStatus.Published;
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HideAsync(int id)
    {
        var place = await _repository.GetByIdAsync(id);
        if (place is null) return false;
        place.Status = PlaceStatus.Hidden;
        await _repository.SaveChangesAsync();
        return true;
    }

    private static AdminPlaceDetailDto ToDetailDto(Place p) => new(
        p.Id, p.Slug, p.Kind.ToString(), p.Category, p.FoodType, p.ParentId,
        p.Latitude, p.Longitude, p.CoverImageUrl, p.Status.ToString(), p.SortOrder,
        p.Address, p.PriceRange, p.OpeningHours);
}