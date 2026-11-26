using AssetForge.Application.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace AssetForge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ProjectService>();
        return services;
    }
}
