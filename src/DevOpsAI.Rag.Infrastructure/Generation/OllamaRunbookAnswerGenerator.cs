using System.Text.Json;
using System.Text.Json.Serialization;
using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Application.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.Infrastructure.Generation;

public sealed class OllamaRunbookAnswerGenerator : IRunbookAnswerGenerator
{
    private static readonly JsonSerializerOptions UserPromptSerializerOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly JsonSerializerOptions ResponseSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

    private readonly IChatClient _chatClient;
    private readonly string _modelId;

    public OllamaRunbookAnswerGenerator(
        IChatClient chatClient,
        IOptions<OllamaChatOptions> options)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Value.Validate();

        _chatClient = chatClient;
        _modelId = options.Value.ModelId.Trim();
    }

    public async Task<GeneratedRunbookAnswer> GenerateAsync(
        RunbookAnswerPrompt prompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var systemMessage = string.Concat(
            prompt.Instructions.SystemPrompt,
            Environment.NewLine,
            Environment.NewLine,
            prompt.Instructions.AnswerFormatPrompt);
        var userMessage = JsonSerializer.Serialize(
            new
            {
                question = prompt.Question,
                evidence = prompt.Evidence.Select(item => new { chunkId = item.ChunkId, text = item.Text })
            },
            UserPromptSerializerOptions);
        var response = await _chatClient
            .GetResponseAsync(
                [
                    new ChatMessage(ChatRole.System, systemMessage),
                    new ChatMessage(ChatRole.User, userMessage)
                ],
                new ChatOptions { ModelId = _modelId, Temperature = 0 },
                cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(response.Text))
        {
            throw new InvalidDataException("The answer provider returned an empty response.");
        }

        AnswerResponse? parsedResponse;
        try
        {
            using var responseDocument = JsonDocument.Parse(response.Text);
            if (responseDocument.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("The answer provider response must be a JSON object.");
            }

            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in responseDocument.RootElement.EnumerateObject())
            {
                if (!propertyNames.Add(property.Name))
                {
                    throw new InvalidDataException("The answer provider response contains duplicate properties.");
                }
            }

            parsedResponse = responseDocument.RootElement.Deserialize<AnswerResponse>(ResponseSerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The answer provider returned an invalid structured response.", exception);
        }

        if (parsedResponse is null
            || string.IsNullOrWhiteSpace(parsedResponse.Answer)
            || parsedResponse.CitedChunkIds is null)
        {
            throw new InvalidDataException("The answer provider returned an incomplete structured response.");
        }

        var status = parsedResponse.Status switch
        {
            "answered" => RunbookAnswerStatus.Answered,
            "insufficient_evidence" => RunbookAnswerStatus.InsufficientEvidence,
            _ => throw new InvalidDataException("The answer provider returned an unsupported answer status.")
        };

        try
        {
            return new GeneratedRunbookAnswer(status, parsedResponse.Answer, parsedResponse.CitedChunkIds);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The answer provider returned invalid citations or answer data.", exception);
        }
    }

    private sealed record AnswerResponse(string? Status, string? Answer, string[]? CitedChunkIds);
}
