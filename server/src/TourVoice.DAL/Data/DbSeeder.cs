using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Data;

/// <summary>
/// Du lieu seed theo PRD: 5 ngon ngu, 4 diem den (dia chi theo phuong moi tu 1/7/2025)
/// va 12 mon [Mau] (6 o Cho Binh Tay, 6 o pho am thuc Phu Lam) - chi dung khi phat trien.
/// Chi co ban tieng Viet (Reviewed); 4 ngon ngu con lai do pipeline MyMemory tao.
/// </summary>
public static class DbSeeder
{
    private record DestinationSeed(string Slug, string Category, decimal Lat, decimal Lng, string Address,
        string Hours, int Order, string Name, string Narration);

    private record FoodSeed(string Slug, string Parent, string FoodType, decimal Lat, decimal Lng,
        string Price, string Hours, int Order, string Name, string Narration);

    private const string FoodCover = "/seed/mon-an.svg";
    private static readonly DestinationSeed[] Destinations =
    [
        new("dinh-binh-tien", "Heritage", 10.750m, 106.642m, "122 Minh Phụng, phường Bình Tây, TP.HCM", "07:00 - 17:00", 1,
            "Đình Bình Tiên",
            "Đình Bình Tiên là một trong những ngôi đình cổ của Quận 6, gắn liền với lịch sử hình thành vùng đất Chợ Lớn xưa. " +
            "Kiến trúc đình mang đậm phong cách truyền thống Nam Bộ với mái ngói âm dương, cột gỗ chạm khắc tinh xảo. " +
            "Hằng năm, đình tổ chức lễ Kỳ Yên để cầu an cho cư dân trong vùng, thu hút đông đảo người dân tham gia."),
        new("cho-binh-tay", "Market", 10.749m, 106.651m, "57A Tháp Mười, phường Bình Tây, TP.HCM", "06:00 - 19:00", 2,
            "Chợ Bình Tây",
            "Chợ Bình Tây, còn gọi là chợ Lớn mới, là khu chợ sỉ lớn bậc nhất Thành phố Hồ Chí Minh. " +
            "Công trình được xây dựng từ đầu thế kỷ 20 theo kiến trúc Á Đông, với tháp đồng hồ đặc trưng ở trung tâm. " +
            "Đây là nơi giao thương sầm uất của cộng đồng người Hoa, bày bán đa dạng hàng hóa từ vải vóc đến thực phẩm khô."),
        new("pho-am-thuc-cu-xa-phu-lam", "FoodStreet", 10.753m, 106.629m, "Cư xá Phú Lâm B, phường Phú Lâm, TP.HCM", "16:00 - 22:00", 3,
            "Phố ẩm thực Cư xá Phú Lâm",
            "Phố ẩm thực Cư xá Phú Lâm là điểm đến quen thuộc của người dân địa phương mỗi buổi chiều tối. " +
            "Con phố tập trung hàng chục quán ăn, xe đẩy bán các món đặc trưng Sài Gòn như bánh xèo, hủ tiếu, chè. " +
            "Không gian bình dân, giá cả phải chăng, mang đến trải nghiệm ẩm thực đường phố đúng chất Nam Bộ."),
        new("cong-vien-binh-phu", "Park", 10.742m, 106.633m, "Cư xá Phú Lâm D, phường Bình Phú, TP.HCM", "05:00 - 21:00", 4,
            "Công viên Bình Phú",
            "Công viên Bình Phú là lá phổi xanh của Quận 6, nơi người dân thường đến tập thể dục, thư giãn mỗi sáng sớm. " +
            "Công viên có hồ nước nhỏ, hàng cây cổ thụ rợp bóng mát và khu vui chơi dành cho trẻ em. " +
            "Vào cuối tuần, nơi đây còn là địa điểm tổ chức các hoạt động cộng đồng, văn nghệ ngoài trời."),
    ];

    // Toa do cach diem den cha khoang 50-150 m, nam trong vung phuc vu (BR-03).
    private static readonly FoodSeed[] Foods =
    [
        new("mau-pho-bo-cho-binh-tay", "cho-binh-tay", "Noodles", 10.7496m, 106.6516m, "40.000đ - 70.000đ", "06:00 - 21:00", 1,
            "[Mẫu] Phở bò", "Tô phở bò nước dùng trong, thơm quế hồi, bánh phở mềm và thịt bò tái chín vừa tới. Đây là món điểm tâm quen thuộc của tiểu thương trong chợ."),
        new("mau-com-tam-suon", "cho-binh-tay", "Rice", 10.7484m, 106.6503m, "35.000đ - 60.000đ", "06:30 - 22:00", 2,
            "[Mẫu] Cơm tấm sườn", "Đĩa cơm tấm với sườn nướng than hồng, bì, chả trứng và mỡ hành. Nước mắm pha chua ngọt là linh hồn của món ăn bình dân này."),
        new("mau-hu-tieu-nam-vang", "cho-binh-tay", "Noodles", 10.7499m, 106.6504m, "45.000đ - 65.000đ", "06:00 - 13:00", 3,
            "[Mẫu] Hủ tiếu Nam Vang", "Hủ tiếu dai với tôm, gan, thịt bằm và nước lèo ngọt thanh từ xương. Có thể ăn khô hoặc nước tùy khẩu vị."),
        new("mau-banh-bao-cho-lon", "cho-binh-tay", "Snack", 10.7483m, 106.6519m, "15.000đ - 30.000đ", "05:30 - 18:00", 4,
            "[Mẫu] Bánh bao Chợ Lớn", "Bánh bao vỏ trắng xốp, nhân thịt, trứng muối và lạp xưởng theo kiểu người Hoa. Món ăn tiện lợi để mang theo khi dạo chợ."),
        new("mau-sam-bo-luong", "cho-binh-tay", "Drink", 10.7492m, 106.6523m, "15.000đ - 25.000đ", "08:00 - 20:00", 5,
            "[Mẫu] Sâm bổ lượng", "Ly sâm bổ lượng mát lạnh với long nhãn, táo đỏ, phổ tai và hạt sen. Thức uống giải nhiệt được nhiều khách ưa chuộng giữa trưa nắng."),
        new("mau-che-tau-xiu", "cho-binh-tay", "Dessert", 10.7488m, 106.6497m, "20.000đ - 35.000đ", "09:00 - 21:00", 6,
            "[Mẫu] Chè Tàu xíu", "Chè kiểu Hoa với mè đen, hạnh nhân và chè trôi nước nóng hổi. Vị ngọt dịu, thơm gừng, rất hợp buổi chiều mát."),
        new("mau-banh-xeo", "pho-am-thuc-cu-xa-phu-lam", "Snack", 10.7536m, 106.6295m, "25.000đ - 40.000đ", "16:00 - 21:00", 1,
            "[Mẫu] Bánh xèo", "Bánh xèo vỏ giòn rụm, nhân tôm thịt và giá đỗ, ăn kèm rau sống tươi. Chấm nước mắm chua ngọt theo công thức gia truyền."),
        new("mau-hu-tieu-mi", "pho-am-thuc-cu-xa-phu-lam", "Noodles", 10.7524m, 106.6284m, "30.000đ - 50.000đ", "17:00 - 22:00", 2,
            "[Mẫu] Hủ tiếu mì", "Hủ tiếu mì bình dân với nước lèo đậm đà ninh từ xương trong nhiều giờ. Là điểm dừng chân quen thuộc của người dân sau giờ làm."),
        new("mau-che-ba-mau", "pho-am-thuc-cu-xa-phu-lam", "Dessert", 10.7539m, 106.6282m, "15.000đ - 25.000đ", "16:00 - 22:00", 3,
            "[Mẫu] Chè ba màu", "Ly chè ba màu với đậu xanh, đậu đỏ, thạch lá dứa và nước cốt dừa béo ngậy. Món tráng miệng yêu thích của cư dân trong khu phố."),
        new("mau-com-chien", "pho-am-thuc-cu-xa-phu-lam", "Rice", 10.7522m, 106.6297m, "35.000đ - 55.000đ", "16:30 - 22:00", 4,
            "[Mẫu] Cơm chiên Dương Châu", "Cơm chiên hạt tơi với lạp xưởng, tôm, trứng và đậu Hà Lan. Phần ăn đầy đặn, giá hợp lý cho bữa tối nhanh."),
        new("mau-nuoc-mia", "pho-am-thuc-cu-xa-phu-lam", "Drink", 10.7531m, 106.6278m, "10.000đ - 20.000đ", "15:00 - 22:00", 5,
            "[Mẫu] Nước mía", "Nước mía ép tại chỗ, thêm chút quất cho thơm và bớt ngọt. Thức uống mát lạnh, rẻ và dễ tìm nhất khu phố."),
        new("mau-bot-chien", "pho-am-thuc-cu-xa-phu-lam", "Snack", 10.7527m, 106.6302m, "25.000đ - 35.000đ", "16:00 - 22:00", 6,
            "[Mẫu] Bột chiên", "Bột chiên giòn bên ngoài, mềm bên trong, phủ trứng và hành lá. Ăn kèm đu đủ ngâm chua và nước tương cay."),
    ];

    public static async Task SeedAsync(TourVoiceDbContext db)
    {
        if (!await db.Languages.AnyAsync())
        {
            db.Languages.AddRange(
                new Language { Code = "vi", MtCode = "vi", SpeechLang = "vi-VN" },
                new Language { Code = "en", MtCode = "en", SpeechLang = "en-US" },
                new Language { Code = "zh", MtCode = "zh-CN", SpeechLang = "zh-CN" },
                new Language { Code = "ko", MtCode = "ko", SpeechLang = "ko-KR" },
                new Language { Code = "ja", MtCode = "ja", SpeechLang = "ja-JP" });
            await db.SaveChangesAsync();
        }

        if (await db.Places.AnyAsync())
        {
            // DB da seed tu truoc: chi bu anh bia con thieu (du lieu cu tao truoc khi co anh).
            await BackfillSeedCoversAsync(db);
            return;
        }

        var parents = new Dictionary<string, Place>();
        foreach (var d in Destinations)
        {
            var place = new Place
            {
                Slug = d.Slug, Kind = PlaceKind.Destination, Category = d.Category,
                Latitude = d.Lat, Longitude = d.Lng, Address = d.Address, OpeningHours = d.Hours,
                CoverImageUrl = $"/seed/{d.Slug}.svg",
                Status = PlaceStatus.Published, SortOrder = d.Order,
                Translations = { Vi(d.Name, d.Narration) },
            };
            parents[d.Slug] = place;
            db.Places.Add(place);
        }

        foreach (var f in Foods)
        {
            var parent = parents[f.Parent];
            db.Places.Add(new Place
            {
                Slug = f.Slug, Kind = PlaceKind.Food, FoodType = f.FoodType, Parent = parent,
                Latitude = f.Lat, Longitude = f.Lng, Address = parent.Address, PriceRange = f.Price, OpeningHours = f.Hours,
                CoverImageUrl = FoodCover,
                Status = PlaceStatus.Published, SortOrder = f.Order,
                Translations = { Vi(f.Name, f.Narration) },
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Ban seed cu (tao truoc khi co anh bia) van phai Publish lai duoc: BR-08 bat buoc CoverImageUrl.
    /// Chi dien cho dong dang trong, khong ghi de anh admin da tai len.
    /// </summary>
    private static async Task BackfillSeedCoversAsync(TourVoiceDbContext db)
    {
        var destinationCovers = Destinations.ToDictionary(d => d.Slug, d => $"/seed/{d.Slug}.svg");
        var foodSlugs = Foods.Select(f => f.Slug).ToHashSet(StringComparer.Ordinal);

        var missing = await db.Places
            .Where(p => p.CoverImageUrl == null || p.CoverImageUrl == "")
            .ToListAsync();

        var changed = false;
        foreach (var place in missing)
        {
            var cover = destinationCovers.TryGetValue(place.Slug, out var own)
                ? own
                : foodSlugs.Contains(place.Slug) ? FoodCover : null;
            if (cover is null) continue;
            place.CoverImageUrl = cover;
            changed = true;
        }

        if (changed) await db.SaveChangesAsync();
    }

    private static PlaceTranslation Vi(string name, string narration) => new()
    {
        LanguageCode = "vi", Name = name, NarrationText = narration,
        Status = TranslationStatus.Reviewed, Source = TranslationSource.Manual,
    };
}
