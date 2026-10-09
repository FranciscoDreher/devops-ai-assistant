using System.Net;
using DevOpsAI.Application;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Infrastructure.AI;

public sealed class MicrosoftExtensionsAiTextGenerator : IAiTextGenerator
{
    private readonly IChatClient _chatClient;
    private readonly AiOptions _options;

    public MicrosoftExtensionsAiTextGenerator(IChatClient chatClient, IOptions<AiOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        _chatClient = chatClient;
        _options = options?.Value ?? AiOptions.FromEnvironment();
    }

    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("AI model is not configured. Set 'AI_MODEL' or 'Ai:Model'.");
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("AI endpoint is not configured. Set 'AI_ENDPOINT' or 'Ai:Endpoint'.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("AI API key is not configured. Set 'AI_API_KEY' or 'Ai:ApiKey'.");
        }

        try
        {
            var response = await _chatClient
                .GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.System, systemPrompt),
                        new ChatMessage(ChatRole.User, userPrompt)
                    ],
                    new ChatOptions { ModelId = _options.Model },
                    cancellationToken)
                .ConfigureAwait(false);

            var generatedText = response?.Text;
            if (string.IsNullOrWhiteSpace(generatedText))
            {
                throw new InvalidOperationException("The AI response text was empty.");
            }

            return generatedText.Trim();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("AI response text was empty", StringComparison.OrdinalIgnoreCase))
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            throw new HttpRequestException(
                $"AI generation failed for model '{_options.Model}'. {ex.Message}",
                ex,
                HttpStatusCode.ServiceUnavailable);
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException or TimeoutException or NotSupportedException)
        {
            throw new HttpRequestException(
                $"AI generation failed for model '{_options.Model}'. {ex.Message}",
                ex,
                HttpStatusCode.ServiceUnavailable);
        }
    }
}
