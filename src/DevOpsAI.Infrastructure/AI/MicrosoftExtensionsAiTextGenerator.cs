using System.Text.Json;
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
            throw new AiGenerationException("AI model is not configured. Set 'AI_MODEL' or 'Ai:Model'.");
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new AiGenerationException("AI endpoint is not configured. Set 'AI_ENDPOINT' or 'Ai:Endpoint'.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new AiGenerationException("AI API key is not configured. Set 'AI_API_KEY' or 'Ai:ApiKey'.");
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
                throw new AiGenerationException("The AI response text was empty.");
            }

            var sanitizedText = generatedText.Trim();
            EnsureStructuredJson(sanitizedText);
            return sanitizedText;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AiGenerationException)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is TaskCanceledException or TimeoutException or NotSupportedException or JsonException)
        {
            throw new AiGenerationException(
                $"AI generation failed for model '{_options.Model}'. {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            throw new AiGenerationException(
                $"AI generation failed for model '{_options.Model}'. {ex.Message}",
                ex);
        }
    }

    private static void EnsureStructuredJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AiGenerationException("The AI response text was empty.");
        }

        var trimmedText = text.TrimStart();
        if (!trimmedText.StartsWith('{') &&
            !trimmedText.StartsWith('['))
        {
            throw new AiGenerationException("The AI response was not valid JSON.");
        }

        using var document = JsonDocument.Parse(text);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new AiGenerationException("The AI response was not a JSON object.");
        }
    }
}
