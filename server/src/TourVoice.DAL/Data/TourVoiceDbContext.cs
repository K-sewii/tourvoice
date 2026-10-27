using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Data;

public class TourVoiceDbContext : DbContext
{
    // Vung phuc vu (BR-03) - chan them bang CHECK o CSDL.
    public const decimal MinLatitude = 10.730m, MaxLatitude = 10.765m;
    public const decimal MinLongitude = 106.615m, MaxLongitude = 106.665m;
    public const int NarrationMaxLength = 4000;

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

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        // Enum luu dang chu cho de doc khi truy van MySQL truc tiep.
        c.Properties<PlaceKind>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<PlaceStatus>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TranslationStatus>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TranslationSource>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<UsageService>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<AccessSource>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Place>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Slug).HasMaxLength(100);
            e.Property(p => p.Category).HasMaxLength(30);
            e.Property(p => p.FoodType).HasMaxLength(30);
            e.Property(p => p.CoverImageUrl).HasMaxLength(500);
            e.Property(p => p.Address).HasMaxLength(300);
            e.Property(p => p.PriceRange).HasMaxLength(100);
            e.Property(p => p.OpeningHours).HasMaxLength(100);
            e.Property(p => p.Latitude).HasColumnType("decimal(9,6)");
            e.Property(p => p.Longitude).HasColumnType("decimal(9,6)");
            e.HasOne(p => p.Parent).WithMany(p => p.Children)
                .HasForeignKey(p => p.ParentId).OnDelete(DeleteBehavior.Restrict);

            // Chi muc theo truy van (PRD/HTML ch.09)
            e.HasIndex(p => new { p.ParentId, p.Status, p.SortOrder }).HasDatabaseName("IX_Places_ParentId_Status_SortOrder");
            e.HasIndex(p => new { p.Status, p.Category }).HasDatabaseName("IX_Places_Status_Category");

            e.ToTable(t =>
            {
                // ADR-09: Destination khong co cha, Food bat buoc co cha.
                t.HasCheckConstraint("CK_Places_Kind_Parent",
                    "(`Kind` = 'Destination' AND `ParentId` IS NULL) OR (`Kind` = 'Food' AND `ParentId` IS NOT NULL)");
                t.HasCheckConstraint("CK_Places_ServiceArea",
                    $"`Latitude` BETWEEN {MinLatitude} AND {MaxLatitude} AND `Longitude` BETWEEN {MinLongitude} AND {MaxLongitude}");
            });
        });

        b.Entity<PlaceTranslation>(e =>
        {
            e.HasKey(t => new { t.PlaceId, t.LanguageCode });
            e.Property(t => t.LanguageCode).HasMaxLength(10);
            e.Property(t => t.Name).HasMaxLength(200);
            e.Property(t => t.NarrationText).HasMaxLength(NarrationMaxLength);
            e.Property(t => t.SourceHash).HasColumnType("char(64)");
            e.Property(t => t.MachineMatch).HasColumnType("decimal(5,4)");
            e.HasOne(t => t.Place).WithMany(p => p.Translations)
                .HasForeignKey(t => t.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Language).WithMany()
                .HasForeignKey(t => t.LanguageCode).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(t => new { t.LanguageCode, t.Status }).HasDatabaseName("IX_PT_LanguageCode_Status");
        });

        b.Entity<Language>(e =>
        {
            e.HasKey(l => l.Code);
            e.Property(l => l.Code).HasMaxLength(10);
            e.Property(l => l.MtCode).HasMaxLength(10);
            e.Property(l => l.SpeechLang).HasMaxLength(10);
        });

        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(254);
            e.Property(u => u.PasswordHash).HasMaxLength(200);
            e.Property(u => u.FullName).HasMaxLength(100);
            e.Property(u => u.PreferredLanguageCode).HasMaxLength(10);
        });

        b.Entity<Favorite>(e =>
        {
            e.HasKey(f => new { f.UserId, f.PlaceId });
            e.HasOne<User>().WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Place).WithMany().HasForeignKey(f => f.PlaceId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Visit>(e =>
        {
            e.HasKey(v => new { v.UserId, v.PlaceId });
            e.HasOne<User>().WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(v => v.Place).WithMany().HasForeignKey(v => v.PlaceId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ListenEvent>(e =>
        {
            e.Property(l => l.LanguageCode).HasMaxLength(10);
            e.HasOne(l => l.Place).WithMany().HasForeignKey(l => l.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(l => new { l.UserId, l.PlayedAt }).HasDatabaseName("IX_ListenEvents_UserId_PlayedAt");
        });

        b.Entity<UsageLog>(e =>
        {
            e.Property(u => u.ErrorCode).HasMaxLength(100);
            e.HasIndex(u => new { u.Service, u.CreatedAt }).HasDatabaseName("IX_UsageLogs_Service_CreatedAt");
        });

        b.Entity<AccessSession>(e =>
        {
            e.Property(a => a.DeviceKey).HasColumnType("char(64)");
            e.HasIndex(a => a.DeviceKey);
            e.HasIndex(a => a.StartedAt).HasDatabaseName("IX_AccessSessions_StartedAt");
            e.HasOne<Place>().WithMany().HasForeignKey(a => a.EntryPlaceId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
