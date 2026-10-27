using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class ListenService : IListenService
{
    public const int MinListenSeconds = 10;    // BR-09
    public const int MaxListenSeconds = 3600;  // bai 4.000 ky tu doc o toc do 0,8 van duoi 10 phut

    private readonly IPlaceRepository _places;
    private readonly ILanguageRepository _languages;
    private readonly IListenEventRepository _listenEvents;

    public ListenService(IPlaceRepository places, ILanguageRepository languages, IListenEventRepository listenEvents)
    {
        _places = places;
        _languages = languages;
        _listenEvents = listenEvents;
    }

    public async Task RecordAsync(ListenEventRequestDto request, int? userId)
    {
        if (request.ListenedSeconds is < MinListenSeconds or > MaxListenSeconds)
            throw new ValidationException("invalid_duration",
                $"Lượt nghe phải từ {MinListenSeconds} đến {MaxListenSeconds} giây.");

        var lang = request.LanguageCode?.Trim().ToLowerInvariant() ?? "";
        if (!(await _languages.GetEnabledAsync()).Any(l => l.Code == lang))
            throw new ValidationException("invalid_language", "Ngôn ngữ không được hỗ trợ.");

        var placeId = await _places.GetPublishedIdBySlugAsync(request.PlaceSlug ?? "")
            ?? throw new PlaceNotFoundException();

        await _listenEvents.AddAsync(new ListenEvent
        {
            PlaceId = placeId,
            LanguageCode = lang,
            UserId = userId,
            ListenedSeconds = request.ListenedSeconds,
        });
    }
}
