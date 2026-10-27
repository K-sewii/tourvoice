using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TourVoice.DAL.Data;

/// <summary>
/// Cho `dotnet ef migrations add/script` chay ma khong can khoi dong API (khong can JWT, secrets).
/// Tao migration khong ket noi CSDL; chuoi ket noi lay tu bien moi truong neu can apply.
/// </summary>
[ExcludeFromCodeCoverage]
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TourVoiceDbContext>
{
    public TourVoiceDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TourVoice")
            ?? "Server=localhost;Port=3306;Database=tourvoice;User=tv;Password=design-time;";
        return new TourVoiceDbContext(new DbContextOptionsBuilder<TourVoiceDbContext>().UseMySQL(conn).Options);
    }
}
