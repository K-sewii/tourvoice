using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class LanguageRepository : ILanguageRepository
{
    // Thu tu co dinh theo PRD (vi, en, zh, ko, ja) thay vi theo khoa chinh.
    private static readonly string[] Order = ["vi", "en", "zh", "ko", "ja"];

    private readonly TourVoiceDbContext _db;
    public LanguageRepository(TourVoiceDbContext db) => _db = db;

    public async Task<List<Language>> GetEnabledAsync() =>
        Sort(await _db.Languages.AsNoTracking().Where(l => l.IsEnabled).ToListAsync());

    public async Task<List<Language>> GetAllAsync() =>
        Sort(await _db.Languages.AsNoTracking().ToListAsync());

    public Task<Language?> GetByCodeAsync(string code) => _db.Languages.FirstOrDefaultAsync(l => l.Code == code);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    private static List<Language> Sort(List<Language> languages) =>
        languages.OrderBy(l => Rank(l.Code)).ThenBy(l => l.Code).ToList();

    private static int Rank(string code)
    {
        var i = Array.IndexOf(Order, code);
        return i < 0 ? int.MaxValue : i;
    }
}
