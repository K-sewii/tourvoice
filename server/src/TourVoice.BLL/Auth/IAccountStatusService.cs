namespace TourVoice.BLL.Auth;

/// <summary>
/// Kiem tra tai khoan con hieu luc tren moi request da xac thuc JWT. JWT khong co refresh token (ADR-06)
/// nen neu chi kiem tra luc dang nhap thi tai khoan bi khoa van dung duoc toi khi token het han.
/// </summary>
public interface IAccountStatusService
{
    /// <summary>False khi tai khoan khong ton tai hoac da bi khoa.</summary>
    Task<bool> IsActiveAsync(int userId, CancellationToken ct = default);
}
