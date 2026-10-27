using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class ListenEventRepository : IListenEventRepository
{
    private readonly TourVoiceDbContext _db;
    public ListenEventRepository(TourVoiceDbContext db) => _db = db;

    public async Task AddAsync(ListenEvent listenEvent)
    {
        _db.ListenEvents.Add(listenEvent);
        await _db.SaveChangesAsync();
    }
}