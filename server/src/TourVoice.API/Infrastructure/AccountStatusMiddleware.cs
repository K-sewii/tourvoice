using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TourVoice.BLL.Auth;

namespace TourVoice.API.Infrastructure;

/// <summary>
/// Khoa tai khoan phai co hieu luc ngay ca khi JWT con han (BR-12, ADR-06 khong co refresh token).
/// Dat sau <c>UseAuthentication</c> de da co ClaimsPrincipal va truoc <c>UseAuthorization</c>.
/// </summary>
public class AccountStatusMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAccountStatusService accounts)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            && !await accounts.IsActiveAsync(userId, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.RequestServices.GetRequiredService<IProblemDetailsService>().TryWriteAsync(new()
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Tài khoản đã bị khóa.",
                    Extensions = { ["code"] = "account_locked" },
                },
            });
            return;
        }

        await next(context);
    }
}
