using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(TourVoiceDbContext db)
    {
        if (!db.Languages.Any())
        {
            db.Languages.AddRange(
                new Language { Code = "vi", TranslatorCode = "vi", VoiceName = "vi-VN-HoaiMyNeural" },
                new Language { Code = "en", TranslatorCode = "en", VoiceName = "en-US-JennyNeural" },
                new Language { Code = "zh", TranslatorCode = "zh-Hans", VoiceName = "zh-CN-XiaoxiaoNeural" },
                new Language { Code = "ko", TranslatorCode = "ko", VoiceName = "ko-KR-SunHiNeural" },
                new Language { Code = "ja", TranslatorCode = "ja", VoiceName = "ja-JP-NanamiNeural" });
        }

        if (!db.Places.Any())
        {
            var dinhBinhTien = new Place { Slug = "dinh-binh-tien", Kind = PlaceKind.Destination, Category = "Heritage",
                Latitude = 10.750m, Longitude = 106.642m, Status = PlaceStatus.Published, SortOrder = 1,
                Address = "25 Phan Văn Khỏe, Phường 2, Quận 6, TP.HCM", OpeningHours = "07:00 - 17:00" };
            var choBinhTay = new Place { Slug = "cho-binh-tay", Kind = PlaceKind.Destination, Category = "Market",
                Latitude = 10.749m, Longitude = 106.651m, Status = PlaceStatus.Published, SortOrder = 2,
                Address = "57A Tháp Mười, Phường 2, Quận 6, TP.HCM", OpeningHours = "06:00 - 19:00" };
            var phoAmThuc = new Place { Slug = "pho-am-thuc-cu-xa-phu-lam", Kind = PlaceKind.Destination, Category = "FoodStreet",
                Latitude = 10.753m, Longitude = 106.629m, Status = PlaceStatus.Published, SortOrder = 3,
                Address = "Cư xá Phú Lâm, Phường 10, Quận 6, TP.HCM", OpeningHours = "16:00 - 22:00" };
            var congVien = new Place { Slug = "cong-vien-binh-phu", Kind = PlaceKind.Destination, Category = "Park",
                Latitude = 10.742m, Longitude = 106.633m, Status = PlaceStatus.Published, SortOrder = 4,
                Address = "Đường Tân Hòa Đông, Phường 13, Quận 6, TP.HCM", OpeningHours = "05:00 - 21:00" };

            db.Places.AddRange(dinhBinhTien, choBinhTay, phoAmThuc, congVien);
            await db.SaveChangesAsync();

            db.Places.AddRange(
                new Place { Slug = "banh-xeo-co-ba", Kind = PlaceKind.Food, FoodType = "Snack",
                    ParentId = phoAmThuc.Id, Latitude = phoAmThuc.Latitude, Longitude = phoAmThuc.Longitude,
                    Status = PlaceStatus.Published, SortOrder = 1,
                    Address = "Cư xá Phú Lâm, Phường 10, Quận 6, TP.HCM",
                    PriceRange = "25.000đ - 40.000đ", OpeningHours = "16:00 - 21:00" },
                new Place { Slug = "hu-tieu-mi-chu-sang", Kind = PlaceKind.Food, FoodType = "Noodles",
                    ParentId = phoAmThuc.Id, Latitude = phoAmThuc.Latitude, Longitude = phoAmThuc.Longitude,
                    Status = PlaceStatus.Published, SortOrder = 2,
                    Address = "Cư xá Phú Lâm, Phường 10, Quận 6, TP.HCM",
                    PriceRange = "30.000đ - 50.000đ", OpeningHours = "17:00 - 22:00" },
                new Place { Slug = "che-co-nam", Kind = PlaceKind.Food, FoodType = "Dessert",
                    ParentId = phoAmThuc.Id, Latitude = phoAmThuc.Latitude, Longitude = phoAmThuc.Longitude,
                    Status = PlaceStatus.Published, SortOrder = 3,
                    Address = "Cư xá Phú Lâm, Phường 10, Quận 6, TP.HCM",
                    PriceRange = "15.000đ - 25.000đ", OpeningHours = "16:00 - 22:00" });

            await db.SaveChangesAsync();
        }

        if (!db.PlaceTranslations.Any())
        {
            var places = await db.Places.ToDictionaryAsync(p => p.Slug, p => p.Id);

            void AddVi(string slug, string name, string narration)
            {
                if (!places.TryGetValue(slug, out var placeId)) return;
                db.PlaceTranslations.Add(new PlaceTranslation
                {
                    PlaceId = placeId,
                    LanguageCode = "vi",
                    Name = name,
                    NarrationText = narration,
                    Status = TranslationStatus.Reviewed,
                    Source = TranslationSource.Human
                });
            }

            AddVi("dinh-binh-tien", "Đình Bình Tiên",
                "Đình Bình Tiên là một trong những ngôi đình cổ của Quận 6, gắn liền với lịch sử hình thành vùng đất Chợ Lớn xưa. " +
                "Kiến trúc đình mang đậm phong cách truyền thống Nam Bộ với mái ngói âm dương, cột gỗ chạm khắc tinh xảo. " +
                "Hằng năm, đình tổ chức lễ Kỳ Yên để cầu an cho cư dân trong vùng, thu hút đông đảo người dân tham gia.");

            AddVi("cho-binh-tay", "Chợ Bình Tây",
                "Chợ Bình Tây, còn gọi là chợ Lớn mới, là khu chợ sỉ lớn bậc nhất Thành phố Hồ Chí Minh. " +
                "Công trình được xây dựng từ đầu thế kỷ 20 theo kiến trúc Á Đông, với tháp đồng hồ đặc trưng ở trung tâm. " +
                "Đây là nơi giao thương sầm uất của cộng đồng người Hoa, bày bán đa dạng hàng hóa từ vải vóc đến thực phẩm khô.");

            AddVi("pho-am-thuc-cu-xa-phu-lam", "Phố ẩm thực Cư xá Phú Lâm",
                "Phố ẩm thực Cư xá Phú Lâm là điểm đến quen thuộc của người dân địa phương mỗi buổi chiều tối. " +
                "Con phố tập trung hàng chục quán ăn, xe đẩy bán các món đặc trưng Sài Gòn như bánh xèo, hủ tiếu, chè. " +
                "Không gian bình dân, giá cả phải chăng, mang đến trải nghiệm ẩm thực đường phố đúng chất Nam Bộ.");

            AddVi("cong-vien-binh-phu", "Công viên Bình Phú",
                "Công viên Bình Phú là lá phổi xanh của Quận 6, nơi người dân thường đến tập thể dục, thư giãn mỗi sáng sớm. " +
                "Công viên có hồ nước nhỏ, hàng cây cổ thụ rợp bóng mát và khu vui chơi dành cho trẻ em. " +
                "Vào cuối tuần, nơi đây còn là địa điểm tổ chức các hoạt động cộng đồng, văn nghệ ngoài trời.");

            AddVi("banh-xeo-co-ba", "Bánh xèo Cô Ba",
                "Quán bánh xèo lâu năm tại Phố ẩm thực Cư xá Phú Lâm, nổi tiếng với lớp vỏ giòn rụm và nhân tôm thịt đầy đặn. " +
                "Bánh được ăn kèm rau sống tươi và nước chấm chua ngọt theo công thức gia truyền.");

            AddVi("hu-tieu-mi-chu-sang", "Hủ tiếu mì Chú Sáng",
                "Quán hủ tiếu mì bình dân, phục vụ từ chiều đến tối muộn với nước lèo đậm đà ninh từ xương trong nhiều giờ. " +
                "Là điểm dừng chân quen thuộc của người dân quanh khu vực sau một ngày làm việc.");

            AddVi("che-co-nam", "Chè Cô Năm",
                "Xe chè nhỏ bán hơn chục loại chè truyền thống như chè đậu xanh, chè bắp, chè thái. " +
                "Vị ngọt thanh, giá bình dân, là món tráng miệng yêu thích của cư dân trong khu phố ẩm thực.");
        }

        await db.SaveChangesAsync();
    }
}