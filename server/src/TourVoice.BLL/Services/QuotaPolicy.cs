namespace TourVoice.BLL.Services;

/// <summary>Hang so han muc MyMemory (BR-06). Mot cho de pipeline va dashboard cung dung.</summary>
public static class QuotaPolicy
{
    public const int DailyLimit = 50_000;                       // co email o tham so de
    public const double BlockRatio = 0.9;
    public const int BlockThreshold = (int)(DailyLimit * BlockRatio);   // 45.000
    /// <summary>So ngay cua muc "trung binh N ngay" tren dashboard va trang han muc.</summary>
    public const int AverageWindowDays = 7;

    /// <summary>Bán kính mặc định hiển thị thiết bị trên map (km)</summary>
    public const int MapRadiusDefaultKm = 5;

    /// <summary>Bán kính tối đa hiển thị thiết bị trên map (km)</summary>
    public const int MapRadiusMaxKm = 50;

    /// <summary>Bán kính tối thiểu hiển thị thiết bị trên map (km)</summary>
    public const int MapRadiusMinKm = 1;

    /// <summary>Viet Nam khong co gio mua he: offset co dinh +7, khong phu thuoc tzdata cua container.</summary>
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    /// <summary>
    /// Thoi diem UTC cua 00:00 hom nay theo gio Viet Nam - moc reset han muc BR-06.
    /// Khong dung <c>DateTime.UtcNow.Date</c>: nhu vay han muc se reset luc 07:00 sang gio VN.
    /// </summary>
    public static DateTime DayStartUtc(TimeProvider clock)
    {
        var nowVn = clock.GetUtcNow().ToOffset(VietnamOffset);
        return new DateTimeOffset(nowVn.Date, VietnamOffset).UtcDateTime;
    }
}
