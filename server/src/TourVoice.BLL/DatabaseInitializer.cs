using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourVoice.DAL.Data;

namespace TourVoice.BLL;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TourVoiceDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }
}