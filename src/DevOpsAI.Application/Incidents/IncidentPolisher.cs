using System.Text.Json;
using DevOpsAI.Domain;

namespace DevOpsAI.Application;

public interface IIncidentPolisher
{
    Task<PolishIncidentResult> PolishAsync(
        PolishIncidentRequest request,
        CancellationToken cancellationToken);
}

public sealed class IncidentPolisher : IIncidentPolisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IAiTextGenerator _aiTextGenerator;
    private readonly IPromptProvider _promptProvider;

    public IncidentPolisher(IAiTextGenerator aiTextGenerator, IPromptProvider promptProvider)
    {
        ArgumentNullException.ThrowIfNull(aiTextGenerator);
        ArgumentNullException.ThrowIfNull(promptProvider);

        _aiTextGenerator = aiTextGenerator;
        _promptProvider = promptProvider;
    }

    public async Task<PolishIncidentResult> PolishAsync(
        PolishIncidentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = IncidentPolishingRules.ValidateRequest(request);
        if (!validation.IsValid)
        {
            return PolishIncidentResult.Failure(validation.Code, validation.Message);
        }

        var prompt = await IncidentPolishingRules
            .CreatePromptAsync(_promptProvider, request.Audience, request.IncidentNote.Trim(), cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var generatedText = await _aiTextGenerator
                .GenerateAsync(prompt.SystemPrompt, prompt.UserPrompt, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                return PolishIncidentResult.Failure(
                    IncidentPolishingRules.InvalidAiOutputCode,
                    "AI output is empty.",
                    prompt);
            }

            var polishedIncident = DeserializeGeneratedIncident(generatedText);
            return PolishIncidentResult.Success(polishedIncident, prompt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AiGenerationException)
        {
            return PolishIncidentResult.Failure(
                IncidentPolishingRules.AiGenerationFailedCode,
                "AI generation failed.",
                prompt);
        }
        catch (JsonException)
        {
            return PolishIncidentResult.Failure(
                IncidentPolishingRules.InvalidAiOutputCode,
                "AI output is malformed.",
                prompt);
        }
        catch (ArgumentException)
        {
            return PolishIncidentResult.Failure(
                IncidentPolishingRules.InvalidAiOutputCode,
                "AI output does not match the expected schema.",
                prompt);
        }
        catch (InvalidOperationException)
        {
            return PolishIncidentResult.Failure(
                IncidentPolishingRules.InvalidAiOutputCode,
                "AI output does not match the expected schema.",
                prompt);
        }
        catch (Exception)
        {
            return PolishIncidentResult.Failure(
                IncidentPolishingRules.AiGenerationFailedCode,
                "AI generation failed.",
                prompt);
        }
    }

    private static PolishedIncident DeserializeGeneratedIncident(string generatedText)
    {
        var generatedIncident = JsonSerializer.Deserialize<GeneratedIncidentResponse>(generatedText, SerializerOptions);
        if (generatedIncident is null)
        {
            throw new InvalidOperationException("The AI response was null.");
        }

        var actionsTaken = generatedIncident.ActionsTaken ?? Array.Empty<string>();
        var nextSteps = generatedIncident.NextSteps ?? Array.Empty<string>();

        return new PolishedIncident(
            generatedIncident.Summary ?? throw new InvalidOperationException("The AI response is missing summary."),
            generatedIncident.Impact ?? throw new InvalidOperationException("The AI response is missing impact."),
            actionsTaken,
            generatedIncident.CurrentStatus ?? throw new InvalidOperationException("The AI response is missing currentStatus."),
            nextSteps);
    }

    private sealed class GeneratedIncidentResponse
    {
        public string? Summary { get; set; }

        public string? Impact { get; set; }

        public string[]? ActionsTaken { get; set; }

        public string? CurrentStatus { get; set; }

        public string[]? NextSteps { get; set; }
    }
}
