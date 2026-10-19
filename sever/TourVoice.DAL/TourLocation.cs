using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace TourVoice.DAL
{
    public class TourLocation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string LocationName { get; set; } = null!;

        // Lưu bản dịch mô tả theo nhiều ngôn ngữ (ví dụ: {"vi": "...", "en": "..."})
        public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();

        public string? AudioGuideUrl { get; set; }
    }
}