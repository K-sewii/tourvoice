using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class LanguageRepository : ILanguageRepository
{
    private readonly TourVoiceDbContext _db;
    public LanguageRepository(TourVoiceDbContext db) => _db = db;

    public async Task<List<Language>> GetEnabledAsync() =>
        await _db.Languages.AsNoTracking().Where(l => l.IsEnabled).ToListAsync();
}