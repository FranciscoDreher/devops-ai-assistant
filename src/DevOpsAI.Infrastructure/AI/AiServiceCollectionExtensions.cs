using DevOpsAI.Application;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsAI.Infrastructure.AI;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .Validate(
                options => options.TimeoutSeconds is > 0 and <= 86_400,
                "Ai:TimeoutSeconds must be between 1 and 86400.")
            .ValidateOnStart();

        services.AddSingleton<IChatClient>(_ => new MissingConfigurationChatClient());
        services.AddScoped<IAiTextGenerator, MicrosoftExtensionsAiTextGenerator>();

        return services;
    }

    private sealed class MissingConfigurationChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            CancellationToken cancellationToken)
        {
            throw new AiGenerationException(
                "AI integration is not configured. Set 'Ai:Endpoint' and 'Ai:ApiKey', or register a provider-specific IChatClient implementation.");
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            CancellationToken cancellationToken)
        {
            throw new AiGenerationException(
                "AI integration is not configured. Set 'Ai:Endpoint' and 'Ai:ApiKey', or register a provider-specific IChatClient implementation.");
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
