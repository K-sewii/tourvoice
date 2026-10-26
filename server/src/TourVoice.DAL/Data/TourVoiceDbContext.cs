using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Data;

public class TourVoiceDbContext : DbContext
{
    public TourVoiceDbContext(DbContextOptions<TourVoiceDbContext> options) : base(options) { }

    public DbSet<Place> Places => Set<Place>();
    public DbSet<PlaceTranslation> PlaceTranslations => Set<PlaceTranslation>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<ListenEvent> ListenEvents => Set<ListenEvent>();
    public DbSet<UsageLog> UsageLogs => Set<UsageLog>();
    public DbSet<AccessSession> AccessSessions => Set<AccessSession>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Place>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Latitude).HasColumnType("decimal(9,6)");
            e.Property(p => p.Longitude).HasColumnType("decimal(9,6)");
            e.HasOne(p => p.Parent).WithMany(p => p.Children)
                .HasForeignKey(p => p.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PlaceTranslation>(e =>
        {
            e.HasKey(t => new { t.PlaceId, t.LanguageCode });
            e.HasOne(t => t.Place).WithMany(p => p.Translations)
                .HasForeignKey(t => t.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Language).WithMany()
                .HasForeignKey(t => t.LanguageCode).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Language>(e => e.HasKey(l => l.Code));

        b.Entity<User>(e => e.HasIndex(u => u.Email).IsUnique());

        b.Entity<Favorite>(e => e.HasKey(f => new { f.UserId, f.PlaceId }));
        b.Entity<Visit>(e => e.HasKey(v => new { v.UserId, v.PlaceId }));

        b.Entity<AccessSession>(e => e.HasIndex(a => a.DeviceKey));
    }
}