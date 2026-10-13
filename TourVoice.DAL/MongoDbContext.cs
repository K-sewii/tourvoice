using MongoDB.Driver;

namespace TourVoice.DAL
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            // Khởi tạo kết nối tới server MongoDB
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // Tạo thuộc tính để lấy ra bộ sưu tập TourLocation
        public IMongoCollection<TourLocation> TourLocations =>
            _database.GetCollection<TourLocation>("Locations");
    }
}