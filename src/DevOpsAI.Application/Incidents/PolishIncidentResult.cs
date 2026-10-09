using DevOpsAI.Domain;

namespace DevOpsAI.Application;

public sealed record IncidentPolishingError(string Code, string Message);

public sealed record PolishIncidentResult
{
    private PolishIncidentResult(bool isSuccess, PolishedIncident? incident, IncidentPolishingError? error, IncidentPrompt? prompt)
    {
        IsSuccess = isSuccess;
        Incident = incident;
        Error = error;
        Prompt = prompt;
    }

    public bool IsSuccess { get; }

    public PolishedIncident? Incident { get; }

    public IncidentPolishingError? Error { get; }

    public IncidentPrompt? Prompt { get; }

    public static PolishIncidentResult Success(PolishedIncident incident, IncidentPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentNullException.ThrowIfNull(prompt);

        return new PolishIncidentResult(true, incident, null, prompt);
    }

    public static PolishIncidentResult Failure(string code, string message, IncidentPrompt? prompt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new PolishIncidentResult(false, null, new IncidentPolishingError(code, message), prompt);
    }
}
