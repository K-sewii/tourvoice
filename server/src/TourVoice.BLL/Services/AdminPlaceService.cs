using System.Text.RegularExpressions;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Storage;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public partial class AdminPlaceService : IAdminPlaceService
{
    public const long MaxCoverBytes = 5 * 1024 * 1024;
    private static readonly string[] Categories = ["Heritage", "Market", "FoodStreet", "Park"];
    private static readonly string[] FoodTypes = ["Noodles", "Rice", "Snack", "Drink", "Dessert"];

    private readonly IPlaceRepository _repository;
    private readonly IFileStorage _storage;

    public AdminPlaceService(IPlaceRepository repository, IFileStorage storage)
    {
        _repository = repository;
        _storage = storage;
    }

    public async Task<PagedResultDto<AdminPlaceNodeDto>> GetTreeAsync(string? status, int page, int pageSize)
    {
        PlaceStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PlaceStatus>(status, ignoreCase: true, out var parsed))
                throw new ValidationException("invalid_status", "Trạng thái không hợp lệ.");
            filter = parsed;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repository.GetAdminTreeAsync(filter, page, pageSize);
        return new PagedResultDto<AdminPlaceNodeDto>(items.Select(ToNode).ToList(), total, page, pageSize);
    }

    public async Task<AdminPlaceDetailDto?> GetByIdAsync(int id)
    {
        var p = await _repository.GetAdminDetailAsync(id);
        return p is null ? null : ToDetailDto(p, includeRelations: true);
    }

    public async Task<AdminPlaceDetailDto> CreateAsync(CreatePlaceRequestDto request)
    {
        var slug = (request.Slug ?? "").Trim();
        ValidateSlug(slug);
        if (!Enum.TryParse<PlaceKind>(request.Kind, ignoreCase: true, out var kind))
            throw new ValidationException("invalid_kind", "Loại địa điểm phải là Destination hoặc Food.");
        ValidateCoordinates(request.Latitude, request.Longitude);

        int? parentId = null;
        if (kind == PlaceKind.Food)
        {
            // BR-02: mon phai la con cua mot diem den.
            var parent = request.ParentId is { } pid ? await _repository.GetByIdAsync(pid) : null;
            if (parent is null || parent.Kind != PlaceKind.Destination)
                throw new BusinessRuleException("invalid_parent", "Món ăn phải thuộc một điểm đến.");
            parentId = parent.Id;
        }
        ValidateClassification(kind, request.Category, request.FoodType);

        if (await _repository.SlugExistsAsync(slug))
            throw new SlugAlreadyExistsException();

        var name = string.IsNullOrWhiteSpace(request.Name) ? slug : request.Name.Trim();
        var place = new Place
        {
            Slug = slug,
            Kind = kind,
            Category = kind == PlaceKind.Destination ? Clean(request.Category) : null,
            FoodType = kind == PlaceKind.Food ? Clean(request.FoodType) : null,
            ParentId = parentId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SortOrder = request.SortOrder,
            Address = Clean(request.Address),
            PriceRange = Clean(request.PriceRange),
            OpeningHours = Clean(request.OpeningHours),
            Status = PlaceStatus.Draft,
        };
        place.Translations.Add(new PlaceTranslation
        {
            LanguageCode = TranslationRules.SourceLanguage,
            Name = name,
            Status = TranslationStatus.Draft,
            Source = TranslationSource.Manual,
        });
        await _repository.AddAsync(place);
        return ToDetailDto(place);
    }

    public async Task<AdminPlaceDetailDto?> UpdateAsync(int id, UpdatePlaceRequestDto request)
    {
        var place = await _repository.GetByIdAsync(id);
        if (place is null) return null;

        ValidateCoordinates(request.Latitude, request.Longitude);
        ValidateClassification(place.Kind, request.Category, request.FoodType);

        var slug = request.Slug?.Trim();
        if (!string.IsNullOrEmpty(slug) && slug != place.Slug)
        {
            if (place.Status != PlaceStatus.Draft) throw new SlugLockedException();   // ma QR da in khong duoc hong
            ValidateSlug(slug);
            if (await _repository.SlugExistsAsync(slug)) throw new SlugAlreadyExistsException();
            place.Slug = slug;
        }

        place.Category = place.Kind == PlaceKind.Destination ? Clean(request.Category) : null;
        place.FoodType = place.Kind == PlaceKind.Food ? Clean(request.FoodType) : null;
        place.Latitude = request.Latitude;
        place.Longitude = request.Longitude;
        place.CoverImageUrl = Clean(request.CoverImageUrl);
        place.SortOrder = request.SortOrder;
        place.Address = Clean(request.Address);
        place.PriceRange = Clean(request.PriceRange);
        place.OpeningHours = Clean(request.OpeningHours);

        await _repository.SaveChangesAsync();
        return ToDetailDto(place);
    }

    public async Task<bool> PublishAsync(int id)
    {
        var place = await _repository.GetByIdAsync(id);
        if (place is null) return false;

        // BR-08
        var missing = new List<string>();
        var vi = place.Translations.FirstOrDefault(t => t.LanguageCode == TranslationRules.SourceLanguage);
        if (vi is not { Status: TranslationStatus.Reviewed } || !TranslationRules.HasText(vi)) missing.Add("vi_narration_reviewed");
        if (!InServiceArea(place.Latitude, place.Longitude)) missing.Add("coordinates");
        if (string.IsNullOrWhiteSpace(place.CoverImageUrl)) missing.Add("cover_image");
        if (place.Kind == PlaceKind.Destination && string.IsNullOrWhiteSpace(place.Category)) missing.Add("category");
        if (place.Kind == PlaceKind.Food && place.ParentId is null) missing.Add("parent");
        if (missing.Count > 0) throw new PublishPreconditionsException(missing);

        place.Status = PlaceStatus.Published;
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HideAsync(int id)
    {
        var place = await _repository.GetByIdAsync(id);
        if (place is null) return false;
        // Mon con cua diem den bi an tu bien mat khoi khach (PlaceRepository.VisibleToGuests), khong doi Status cua chung.
        place.Status = PlaceStatus.Hidden;
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var place = await _repository.GetByIdAsync(id);
        if (place is null) return false;

        // BR-13
        if (place.Status != PlaceStatus.Draft)
            throw new DeleteNotAllowedException("place_not_draft", "Chỉ xóa được địa điểm ở trạng thái Draft.");
        if (await _repository.HasChildrenAsync(id))
            throw new DeleteNotAllowedException("place_has_children", "Hãy xóa các món ăn con trước.");

        await _repository.RemoveAsync(place);
        if (place.CoverImageUrl is not null) await _storage.DeleteAsync(place.CoverImageUrl);
        return true;
    }

    public async Task<AdminPlaceDetailDto?> SetCoverAsync(int id, Stream content, long length)
    {
        var place = await _repository.GetByIdAsync(id);
        if (place is null) return null;

        if (length <= 0) throw new ValidationException("empty_file", "File rỗng.");
        if (length > MaxCoverBytes) throw new ValidationException("file_too_large", "Ảnh bìa tối đa 5 MB.");

        var header = new byte[12];
        var read = 0;
        while (read < header.Length)
        {
            var n = await content.ReadAsync(header.AsMemory(read));
            if (n == 0) break;
            read += n;
        }
        var extension = DetectImageExtension(header.AsSpan(0, read))
            ?? throw new ValidationException("invalid_image", "Chỉ nhận ảnh JPG, PNG hoặc WebP.");

        // Doc lai tu dau: header da bi tieu thu, nen ghep lai truoc khi luu.
        using var full = new MemoryStream();
        full.Write(header, 0, read);
        await content.CopyToAsync(full);
        if (full.Length > MaxCoverBytes) throw new ValidationException("file_too_large", "Ảnh bìa tối đa 5 MB.");
        full.Position = 0;

        var oldUrl = place.CoverImageUrl;
        place.CoverImageUrl = await _storage.SaveAsync(full, "covers", extension);
        await _repository.SaveChangesAsync();
        if (oldUrl is not null) await _storage.DeleteAsync(oldUrl);
        return ToDetailDto(place);
    }

    // ---- helpers ----

    internal static string? DetectImageExtension(ReadOnlySpan<byte> h)
    {
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
        if (h.Length >= 8 && h[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h.Slice(8, 4).SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateSlug(string slug)
    {
        if (slug.Length is 0 or > 100 || !SlugPattern().IsMatch(slug))
            throw new ValidationException("invalid_slug", "Slug chỉ gồm chữ thường, số và dấu gạch ngang (tối đa 100 ký tự).");
    }

    // BR-03
    private static bool InServiceArea(decimal lat, decimal lng) =>
        lat is >= TourVoiceDbContext.MinLatitude and <= TourVoiceDbContext.MaxLatitude
        && lng is >= TourVoiceDbContext.MinLongitude and <= TourVoiceDbContext.MaxLongitude;

    private static void ValidateCoordinates(decimal lat, decimal lng)
    {
        if (!InServiceArea(lat, lng)) throw new OutsideServiceAreaException();
    }

    private static void ValidateClassification(PlaceKind kind, string? category, string? foodType)
    {
        if (kind == PlaceKind.Destination && category is not null && !Categories.Contains(category))
            throw new ValidationException("invalid_category", $"Danh mục phải là một trong: {string.Join(", ", Categories)}.");
        if (kind == PlaceKind.Food && foodType is not null && !FoodTypes.Contains(foodType))
            throw new ValidationException("invalid_food_type", $"Loại món phải là một trong: {string.Join(", ", FoodTypes)}.");
    }

    private static AdminPlaceNodeDto ToNode(Place p)
    {
        var vi = p.Translations.FirstOrDefault(t => t.LanguageCode == TranslationRules.SourceLanguage);
        var withText = p.Translations.Where(TranslationRules.HasText).ToList();
        return new AdminPlaceNodeDto(
            p.Id, p.Slug, vi?.Name ?? p.Slug, p.Kind.ToString(), p.Category, p.FoodType,
            p.Status.ToString(), p.SortOrder, p.CoverImageUrl,
            TranslationRules.HasText(vi),
            withText.Count(t => t.Status == TranslationStatus.Reviewed),
            withText.Count(t => t.Status == TranslationStatus.Draft),
            p.Children.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).Select(ToNode).ToList());
    }

    private static AdminPlaceDetailDto ToDetailDto(Place p, bool includeRelations = false)
    {
        var vi = p.Translations.FirstOrDefault(t => t.LanguageCode == TranslationRules.SourceLanguage);
        return new AdminPlaceDetailDto(
            p.Id, p.Slug, p.Kind.ToString(), p.Category, p.FoodType, p.ParentId,
            p.Latitude, p.Longitude, p.CoverImageUrl, p.Status.ToString(), p.SortOrder,
            p.Address, p.PriceRange, p.OpeningHours,
            SlugLocked: p.Status != PlaceStatus.Draft,
            ParentName: p.Parent?.Translations.FirstOrDefault(t => t.LanguageCode == TranslationRules.SourceLanguage)?.Name,
            Translations: p.Translations.OrderBy(t => t.LanguageCode).Select(t => ToTranslationDto(t, vi)).ToList(),
            Foods: includeRelations ? p.Children.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).Select(ToNode).ToList() : null);
    }

    internal static AdminTranslationDto ToTranslationDto(PlaceTranslation t, PlaceTranslation? vi) => new(
        t.LanguageCode, t.Name, t.NarrationText, t.Status.ToString(), t.Source.ToString(),
        TranslationRules.IsStale(t, vi), t.MachineMatch);

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
