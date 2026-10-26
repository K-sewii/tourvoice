using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourVoice.DAL;

namespace TourVoice.BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(
        this IServiceCollection services, IConfiguration configuration)
    {
        // BLL tự kéo DAL vào, API không cần biết DAL tồn tại
        services.AddDataAccessLayer(configuration);

        // Bước 4: đăng ký các service nghiệp vụ (PlaceService, ...) tại đây
        return services;
    }
}