using DevOpsAI.Domain;

namespace DevOpsAI.Application;

public static class IncidentPolishingRules
{
    public const int MaxIncidentNoteLength = 4000;
    public const string InvalidIncidentNoteCode = "INVALID_INCIDENT_NOTE";
    public const string InvalidAudienceCode = "INVALID_AUDIENCE";
    public const string InvalidAiOutputCode = "INVALID_AI_OUTPUT";
    public const string AiGenerationFailedCode = "AI_GENERATION_FAILED";

    public static async Task<IncidentPrompt> CreatePromptAsync(
        IPromptProvider promptProvider,
        IncidentAudience audience,
        string incidentNote,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(promptProvider);

        var cleanNote = incidentNote.Trim();
        var audienceName = audience.ToString();
        var systemPrompt = await promptProvider
            .GetSystemPromptAsync(cancellationToken)
            .ConfigureAwait(false);
        var audiencePrompt = await promptProvider
            .GetAudiencePromptAsync(audience, cancellationToken)
            .ConfigureAwait(false);

        var userPrompt = $"""
            Audience: {audienceName}
            Incident note:
            {cleanNote}

            {audiencePrompt}

            Write a concise update in valid JSON with these fields:
            - summary
            - impact
            - actionsTaken
            - currentStatus
            - nextSteps
            """;

        return new IncidentPrompt(audienceName, systemPrompt, userPrompt);
    }

    private static string GetAudienceGuidance(IncidentAudience audience)
    {
        return audience switch
        {
            IncidentAudience.Leadership => "Provide an executive briefing focused on business impact, operational status, and the most relevant decisions or risks.",
            IncidentAudience.Customer => "Write a customer-facing update with reassurance, clear status, and a practical explanation without unnecessary technical detail.",
            _ => throw new ArgumentOutOfRangeException(nameof(audience), audience, "Unsupported incident audience.")
        };
    }

    public static (bool IsValid, string Code, string Message) ValidateRequest(PolishIncidentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.IncidentNote))
        {
            return (false, InvalidIncidentNoteCode, "IncidentNote is required.");
        }

        var normalizedIncidentNote = request.IncidentNote.Trim();
        if (normalizedIncidentNote.Length == 0 || normalizedIncidentNote.Length > MaxIncidentNoteLength)
        {
            return (false, InvalidIncidentNoteCode, $"IncidentNote must be between 1 and {MaxIncidentNoteLength} characters.");
        }

        if (!Enum.IsDefined(request.Audience))
        {
            return (false, InvalidAudienceCode, "Audience is unsupported.");
        }

        return (true, string.Empty, string.Empty);
    }
}
