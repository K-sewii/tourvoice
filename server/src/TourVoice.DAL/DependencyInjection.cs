using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourVoice.DAL.Data;
using TourVoice.DAL.Presence;
using TourVoice.DAL.Repositories;

namespace TourVoice.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessLayer(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TourVoice")
            ?? throw new InvalidOperationException("Thieu ConnectionStrings:TourVoice.");

        services.AddDbContext<TourVoiceDbContext>(options =>
            options.UseMySQL(connectionString));

        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IUsageLogRepository, UsageLogRepository>();
        services.AddScoped<IListenEventRepository, ListenEventRepository>();
        services.AddScoped<IAccessSessionRepository, AccessSessionRepository>();
        services.AddScoped<IStatsRepository, StatsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPresenceStore, InMemoryPresenceStore>();
        return services;
    }
}
