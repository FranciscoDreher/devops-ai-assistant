using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsAI.Application;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(configuration);
        services.AddSingleton<IAiTextGenerator, ConfigurationAiTextGenerator>();
        return services;
    }

    private sealed class ConfigurationAiTextGenerator : IAiTextGenerator
    {
        private readonly IConfiguration _configuration;

        public ConfigurationAiTextGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<string> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
            ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

            var model = _configuration["Ai:Model"];
            var endpoint = _configuration["Ai:Endpoint"];
            var apiKey = _configuration["Ai:ApiKey"];

            if (string.IsNullOrWhiteSpace(model) ||
                string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(apiKey))
            {
                throw new AiGenerationException(
                    "AI configuration is incomplete. Set 'Ai:Model', 'Ai:Endpoint', and 'Ai:ApiKey' before calling the generator.");
            }

            throw new AiGenerationException(
                "A provider-specific IChatClient implementation is required to execute the AI request. Register the infrastructure adapter in the composition root.");
        }
    }
}
