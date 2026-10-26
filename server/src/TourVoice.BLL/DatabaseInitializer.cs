using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.BLL;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TourVoiceDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var hasher = new PasswordHasher<User>();
            var admin = new User
            {
                Email = "admin@tourvoice.local",
                FullName = "Quan tri vien",
                Role = UserRole.Admin
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin@12345");
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}