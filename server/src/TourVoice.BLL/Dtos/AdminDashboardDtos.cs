namespace TourVoice.BLL.Dtos;

/// <param name="Percent">UsedToday / DailyLimit * 100 (de ve thanh; vach chan nam o BlockThreshold).</param>
public record QuotaDto(int UsedToday, int DailyLimit, int BlockThreshold, double Percent, int Average7Days);

/// <param name="Date">Ngay theo gio Asia/Ho_Chi_Minh, dinh dang yyyy-MM-dd.</param>
public record DayStatDto(string Date, int Listens, int Sessions, int QrScans, int PeakConcurrent);

public record DashboardDto(
    int Live, int PublishedPlaces, int DraftTranslations, DayStatDto Today, QuotaDto Quota, List<DayStatDto> Days);

public record AccessStatsDto(List<DayStatDto> Days);

public record UsageLogDto(DateTime CreatedAt, int Characters, int Chunks, bool Success, string? ErrorCode);

public record UsageDto(QuotaDto Quota, List<UsageLogDto> Recent);
