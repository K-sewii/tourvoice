using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourVoice.BLL.Services;
using TourVoice.DAL;

namespace TourVoice.BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(
        this IServiceCollection services, IConfiguration configuration)
    {
        // BLL tu keo DAL vao, API khong can biet DAL ton tai
        services.AddDataAccessLayer(configuration);

        services.AddScoped<IPlaceService, PlaceService>();
        services.AddScoped<ILanguageService, LanguageService>();

        return services;
    }
}