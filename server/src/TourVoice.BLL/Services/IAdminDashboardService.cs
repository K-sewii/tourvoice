using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IAdminDashboardService
{
    /// <summary>SCR-A2: dang truy cap, luot nghe, luot quet QR, han muc MyMemory, bieu do 14 ngay.</summary>
    Task<DashboardDto> GetDashboardAsync();

    /// <summary>Thong ke truy cap theo ngay; dinh dong thoi tinh lai tu AccessSessions.</summary>
    Task<AccessStatsDto> GetAccessStatsAsync(int days);

    /// <summary>SCR-A8: han muc hom nay, trung binh 7 ngay, 50 lan goi gan nhat.</summary>
    Task<UsageDto> GetUsageAsync();
}
