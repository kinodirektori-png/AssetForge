using AssetForge.Application.Projects;

namespace AssetForge.Api;

public static class ServiceRegistration
{
    /// <summary>Registers the use cases of the Application layer, which has no reference to the DI container.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ProjectService>();
        return services;
    }
}
