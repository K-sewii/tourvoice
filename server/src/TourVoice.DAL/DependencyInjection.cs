using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TourVoice.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessLayer(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Bước 3: đăng ký DbContext (SQL Server) và các repository tại đây
        return services;
    }
}