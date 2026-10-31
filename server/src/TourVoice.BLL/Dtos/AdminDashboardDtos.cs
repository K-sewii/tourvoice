namespace TourVoice.BLL.Dtos;

/// <param name="Percent">UsedToday / DailyLimit * 100 (de ve thanh; vach chan nam o BlockThreshold).</param>
public record QuotaDto(int UsedToday, int DailyLimit, int BlockThreshold, double Percent, int Average7Days, int AverageDays);

/// <param name="Date">Ngay theo gio Asia/Ho_Chi_Minh, dinh dang yyyy-MM-dd.</param>
public record DayStatDto(string Date, int Listens, int Sessions, int QrScans, int PeakConcurrent);

/// <param name="RefreshSeconds">Chu ky server quet presence (PresenceService.SweepInterval).</param>
/// <param name="TtlSeconds">Thiet bi im lang qua thoi gian nay thi bi coi la offline (BR-15).</param>
public record PresenceSettingsDto(int RefreshSeconds, int TtlSeconds);

public record DashboardDto(
    int Live, int PublishedPlaces, int DraftTranslations, DayStatDto Today, QuotaDto Quota,
    List<DayStatDto> Days, PresenceSettingsDto Presence, int MapRadiusKm = 5, int LiveDeviceCount = 0);

public record AccessStatsDto(List<DayStatDto> Days);

public record UsageLogDto(DateTime CreatedAt, int Characters, int Chunks, bool Success, string? ErrorCode);

/// <param name="RecentLimit">So dong nhat ky gan nhat server tra ve (AdminDashboardService.RecentLogCount).</param>
public record UsageDto(QuotaDto Quota, List<UsageLogDto> Recent, int RecentLimit);
