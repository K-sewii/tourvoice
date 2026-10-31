using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TourVoice.BLL.Exceptions;

namespace TourVoice.API.Infrastructure;

/// <summary>
/// Anh xa duy nhat Exception -> HTTP (HTML ch.01). Controller khong try/catch; moi loi deu thanh
/// RFC 7807 ProblemDetails kem <c>code</c> va <c>traceId</c>, khong bao gio lo stack trace (NFR-04).
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, code, title) = exception switch
        {
            // Client da ngat ket noi (dong tab/huy request): khong phai loi he thong, khong co ai de tra loi.
            OperationCanceledException when http.RequestAborted.IsCancellationRequested
                => (StatusCodes.Status499ClientClosedRequest, "client_closed_request", "Yêu cầu đã bị huỷ."),
            ValidationException e => (StatusCodes.Status400BadRequest, e.Code, e.Message),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, e.Code, e.Message),
            ForbiddenException e => (StatusCodes.Status403Forbidden, e.Code, e.Message),
            NotFoundException e => (StatusCodes.Status404NotFound, e.Code, e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, e.Code, e.Message),
            BusinessRuleException e => (StatusCodes.Status422UnprocessableEntity, e.Code, e.Message),
            QuotaExceededException e => (StatusCodes.Status429TooManyRequests, e.Code, e.Message),
            ExternalServiceException e => (StatusCodes.Status502BadGateway, e.Code, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "internal_error", "Đã có lỗi xảy ra."),
        };

        if (status >= 500) logger.LogError(exception, "Unhandled error {Code}", code);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Extensions = { ["code"] = code },
        };
        if (exception is PublishPreconditionsException publish) problem.Extensions["missing"] = publish.Missing;

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = problem,
        });
    }
}
