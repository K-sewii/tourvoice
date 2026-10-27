using System.Security.Claims;

namespace TourVoice.API.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public const string CustomerRole = "Customer";
    public const string AdminRole = "Admin";

    /// <summary>Id nguoi dung tu JWT; null voi khach vang lai.</summary>
    public static int? UserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>Id khi request kem JWT role Customer (PRD: isFavorite/isVisited chi cho Customer).</summary>
    public static int? CustomerId(this ClaimsPrincipal user) => user.IsInRole(CustomerRole) ? user.UserId() : null;

    /// <summary>Endpoint da [Authorize] nen luon co id.</summary>
    public static int RequiredUserId(this ClaimsPrincipal user) =>
        user.UserId() ?? throw new InvalidOperationException("Endpoint can [Authorize].");
}
