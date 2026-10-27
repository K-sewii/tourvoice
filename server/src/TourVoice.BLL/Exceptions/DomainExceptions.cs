namespace TourVoice.BLL.Exceptions;

/// <summary>
/// Loi nghiep vu cua BLL. Presentation anh xa duy nhat mot lan sang ProblemDetails
/// (HTML ch.01): Validation 400, Unauthorized 401, Forbidden 403, NotFound 404,
/// Conflict 409, BusinessRule 422, QuotaExceeded 429, ExternalService 502.
/// <see cref="Code"/> la ma on dinh de frontend xu ly (vd. "quota_exceeded").
/// </summary>
public abstract class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public class ValidationException(string code, string message) : DomainException(code, message);
public class UnauthorizedException(string code, string message) : DomainException(code, message);
public class ForbiddenException(string code, string message) : DomainException(code, message);
public class NotFoundException(string code, string message) : DomainException(code, message);
public class ConflictException(string code, string message) : DomainException(code, message);
public class BusinessRuleException(string code, string message) : DomainException(code, message);
public class QuotaExceededException(string message = "Đã chạm 90% hạn mức MyMemory hôm nay.")
    : DomainException("quota_exceeded", message);
public class ExternalServiceException(string service, string message)
    : DomainException("external_service_failed", $"{service}: {message}");

// Ten cu giu lai de code admin (vong sau) khong phai doi ngay.
public class PlaceNotFoundException() : NotFoundException("place_not_found", "Không tìm thấy địa điểm.");
public class EmailAlreadyExistsException() : ConflictException("email_taken", "Email đã được sử dụng.");
public class SlugAlreadyExistsException() : ConflictException("slug_taken", "Slug đã tồn tại.");
public class MissingSourceNarrationException()
    : BusinessRuleException("source_not_reviewed", "Bản tiếng Việt chưa có hoặc chưa được duyệt.");


// Vong admin (BR-02, 03, 08, 11, 13)
/// <summary>BR-08: 422 kem danh sach muc con thieu (vi_narration_reviewed, coordinates, cover_image, category, parent).</summary>
public class PublishPreconditionsException(IReadOnlyList<string> missing)
    : BusinessRuleException("publish_preconditions_failed", "Chưa đủ điều kiện xuất bản.")
{
    public IReadOnlyList<string> Missing { get; } = missing;
}
public class SlugLockedException() : BusinessRuleException("slug_locked", "Slug đã khóa vì địa điểm đã xuất bản.");
public class OutsideServiceAreaException()
    : BusinessRuleException("outside_service_area", "Tọa độ nằm ngoài vùng phục vụ Quận 6.");
public class DeleteNotAllowedException(string code, string message) : BusinessRuleException(code, message);
public class LastAdminException() : BusinessRuleException("last_admin", "Phải luôn còn ít nhất một Admin đang hoạt động.");
public class LanguageNotFoundException() : NotFoundException("language_not_found", "Không tìm thấy ngôn ngữ.");
public class UserNotFoundException() : NotFoundException("user_not_found", "Không tìm thấy tài khoản.");
