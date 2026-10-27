using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;
using TourVoice.BLL.Auth;
using TourVoice.BLL.Presence;
using TourVoice.BLL.Services;
using TourVoice.BLL.Translation;
using TourVoice.DAL;

namespace TourVoice.BLL;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataAccessLayer(configuration);

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddScoped<IPlaceService, PlaceService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAdminPlaceService, AdminPlaceService>();
        services.AddScoped<INarrationPipelineService, NarrationPipelineService>();
        services.AddScoped<IListenService, ListenService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IPresenceService, PresenceService>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient<ITranslationProvider, MyMemoryTranslationProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.mymemory.translated.net/");
        });

        return services;
    }
}