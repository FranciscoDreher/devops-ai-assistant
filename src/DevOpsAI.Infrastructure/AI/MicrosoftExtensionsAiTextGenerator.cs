using System.Diagnostics;
using System.Text.Json;
using DevOpsAI.Application;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Infrastructure.AI;

public sealed class MicrosoftExtensionsAiTextGenerator : IAiTextGenerator
{
    private static readonly Action<ILogger, string, string, double, string, Exception?> GenerationFinished =
        LoggerMessage.Define<string, string, double, string>(
            LogLevel.Information,
            new EventId(1, nameof(GenerationFinished)),
            "AI generation {Outcome} for model {Model} in {ElapsedMilliseconds} ms. TraceId: {TraceId}");

    private readonly IChatClient _chatClient;
    private readonly AiOptions _options;
    private readonly ILogger _logger;

    public MicrosoftExtensionsAiTextGenerator(
        IChatClient chatClient,
        IOptions<AiOptions>? options = null,
        ILogger<MicrosoftExtensionsAiTextGenerator>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        _chatClient = chatClient;
        _options = options?.Value ?? AiOptions.FromEnvironment();
        _logger = logger ?? NullLogger<MicrosoftExtensionsAiTextGenerator>.Instance;
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

        if (!AiOptions.IsValidTimeout(_options.TimeoutSeconds))
        {
            throw new AiGenerationException($"AI timeout configuration must be between {AiOptions.MinTimeoutSeconds} and {AiOptions.MaxTimeoutSeconds} seconds.");
        }

        var startedTimestamp = Stopwatch.GetTimestamp();
        var outcome = "failed";
        using var timeoutCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellationSource.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            var response = await _chatClient
                .GetResponseAsync(
                    [
                        new ChatMessage(ChatRole.System, systemPrompt),
                        new ChatMessage(ChatRole.User, userPrompt)
                    ],
                    new ChatOptions { ModelId = _options.Model },
                    timeoutCancellationSource.Token)
                .ConfigureAwait(false);

            var generatedText = response?.Text;
            if (string.IsNullOrWhiteSpace(generatedText))
            {
                throw new AiGenerationException("The AI response text was empty.");
            }

            var sanitizedText = generatedText.Trim();
            EnsureStructuredJson(sanitizedText);
            outcome = "succeeded";
            return sanitizedText;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outcome = "cancelled";
            throw;
        }
        catch (OperationCanceledException) when (timeoutCancellationSource.IsCancellationRequested)
        {
            outcome = "timed out";
            throw new AiGenerationException("AI generation timed out.");
        }
        catch (AiGenerationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AiGenerationException("AI generation failed.");
        }
        finally
        {
            GenerationFinished(
                _logger,
                outcome,
                _options.Model,
                Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds,
                Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity ? activity.TraceId.ToString() : string.Empty,
                null);
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
