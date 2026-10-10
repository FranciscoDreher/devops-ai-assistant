using DevOpsAI.Application;

namespace DevOpsAI.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddIncidentApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IPromptProvider, ApiPromptProvider>();
        services.AddScoped<IIncidentPolisher, IncidentPolisher>();
        services.AddAiServices(configuration);

        return services;
    }
}
