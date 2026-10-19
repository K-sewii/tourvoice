using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TourVoice.DAL
{
    public class TourLocationRepository
    {
        private readonly IMongoCollection<TourLocation> _tourLocations;

        public TourLocationRepository(MongoDbContext context)
        {
            _tourLocations = context.TourLocations;
        }

        public async Task<List<TourLocation>> GetAllAsync()
        {
            return await _tourLocations.Find(_ => true).ToListAsync();
        }

        public async Task CreateAsync(TourLocation location)
        {
            await _tourLocations.InsertOneAsync(location);
        }
    }
}