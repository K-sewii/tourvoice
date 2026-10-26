using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourVoice.DAL.Data;
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
            options.UseSqlServer(connectionString));

        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        return services;
    }
}