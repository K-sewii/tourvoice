namespace TourVoice.BLL.Services;

/// <summary>Hang so han muc MyMemory (BR-06). Mot cho de pipeline va dashboard cung dung.</summary>
public static class QuotaPolicy
{
    public const int DailyLimit = 50_000;                       // co email o tham so de
    public const double BlockRatio = 0.9;
    public const int BlockThreshold = (int)(DailyLimit * BlockRatio);   // 45.000
}
