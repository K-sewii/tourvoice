using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Localization;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IPlaceRepository _places;

    public CustomerService(ICustomerRepository customers, IPlaceRepository places)
    {
        _customers = customers;
        _places = places;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await _customers.GetUserByIdAsync(userId)
            ?? throw new UnauthorizedException("account_missing", "Tài khoản không còn tồn tại.");
        return new UserProfileDto(user.Id, user.Email, user.FullName, user.Role.ToString(), user.PreferredLanguageCode);
    }

    public async Task<List<FavoritePlaceDto>> GetFavoritesAsync(int userId, string lang) =>
        (await _customers.GetFavoritesAsync(userId))
            .Select(f => new FavoritePlaceDto(ToMyPlace(f.Place, lang), f.CreatedAt)).ToList();

    public async Task AddFavoriteAsync(int userId, int placeId)
    {
        await EnsurePublishedAsync(placeId);
        await _customers.AddFavoriteAsync(userId, placeId);
    }

    public Task RemoveFavoriteAsync(int userId, int placeId) => _customers.RemoveFavoriteAsync(userId, placeId);

    public async Task<VisitsDto> GetVisitsAsync(int userId, string lang)
    {
        var visits = await _customers.GetVisitsAsync(userId);
        var published = await _places.CountPublishedDestinationsAsync();
        var visitedDestinations = visits.Count(v =>
            v.Place.Kind == PlaceKind.Destination && v.Place.Status == PlaceStatus.Published);

        return new VisitsDto(
            visits.Select(v => new VisitedPlaceDto(ToMyPlace(v.Place, lang), v.VisitedAt)).ToList(),
            visitedDestinations, published);
    }

    public async Task MarkVisitedAsync(int userId, int placeId)
    {
        await EnsurePublishedAsync(placeId);
        await _customers.MarkVisitedAsync(userId, placeId);
    }

    public async Task<List<ListenHistoryDto>> GetHistoryAsync(int userId, string lang) =>
        (await _customers.GetHistoryAsync(userId))
            .Select(h => new ListenHistoryDto(ToMyPlace(h.Place, lang), h.LanguageCode, h.ListenedSeconds, h.PlayedAt))
            .ToList();

    private async Task EnsurePublishedAsync(int placeId)
    {
        if (!await _places.IsPublishedAsync(placeId)) throw new PlaceNotFoundException();
    }

    private static MyPlaceDto ToMyPlace(Place p, string lang) =>
        new(p.Id, p.Slug, Localizer.Name(p, lang), p.Kind.ToString(), p.Parent?.Slug);
}
