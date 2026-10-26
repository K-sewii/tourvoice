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
            db.Places.AddRange(
                new Place { Slug = "dinh-binh-tien", Kind = PlaceKind.Destination, Category = "Heritage",
                    Latitude = 10.750m, Longitude = 106.642m, Status = PlaceStatus.Published, SortOrder = 1 },
                new Place { Slug = "cho-binh-tay", Kind = PlaceKind.Destination, Category = "Market",
                    Latitude = 10.749m, Longitude = 106.651m, Status = PlaceStatus.Published, SortOrder = 2 },
                new Place { Slug = "pho-am-thuc-cu-xa-phu-lam", Kind = PlaceKind.Destination, Category = "FoodStreet",
                    Latitude = 10.753m, Longitude = 106.629m, Status = PlaceStatus.Published, SortOrder = 3 },
                new Place { Slug = "cong-vien-binh-phu", Kind = PlaceKind.Destination, Category = "Park",
                    Latitude = 10.742m, Longitude = 106.633m, Status = PlaceStatus.Published, SortOrder = 4 });
        }

        await db.SaveChangesAsync();
    }
}