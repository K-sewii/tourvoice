using TourVoice.BLL.Dtos;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class ListenService : IListenService
{
    private readonly IPlaceRepository _placeRepository;
    private readonly IListenEventRepository _listenEventRepository;

    public ListenService(IPlaceRepository placeRepository, IListenEventRepository listenEventRepository)
    {
        _placeRepository = placeRepository;
        _listenEventRepository = listenEventRepository;
    }

    public async Task<bool> RecordListenAsync(string slug, RecordListenRequestDto request, int? userId)
    {
        var placeId = await _placeRepository.GetPublishedIdBySlugAsync(slug);
        if (placeId is null) return false;

        var source = Enum.TryParse<ListenSource>(request.Source, ignoreCase: true, out var parsed)
            ? parsed : ListenSource.Mp3;

        await _listenEventRepository.AddAsync(new ListenEvent
        {
            PlaceId = placeId.Value,
            LanguageCode = request.LanguageCode,
            UserId = userId,
            ListenedSeconds = request.ListenedSeconds,
            Source = source
        });
        return true;
    }
}