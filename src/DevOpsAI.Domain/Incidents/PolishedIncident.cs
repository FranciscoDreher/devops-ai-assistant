namespace DevOpsAI.Domain;

public sealed record PolishedIncident
{
    public PolishedIncident(
        string summary,
        string impact,
        IReadOnlyList<string> actionsTaken,
        string currentStatus,
        IReadOnlyList<string> nextSteps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentException.ThrowIfNullOrWhiteSpace(impact);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentStatus);
        ArgumentNullException.ThrowIfNull(actionsTaken);
        ArgumentNullException.ThrowIfNull(nextSteps);

        Summary = summary.Trim();
        Impact = impact.Trim();
        CurrentStatus = currentStatus.Trim();

        ActionsTaken = ValidateNonEmptyTextList(actionsTaken, nameof(actionsTaken));
        NextSteps = ValidateNonEmptyTextList(nextSteps, nameof(nextSteps));
    }

    public string Summary { get; }

    public string Impact { get; }

    public IReadOnlyList<string> ActionsTaken { get; }

    public string CurrentStatus { get; }

    public IReadOnlyList<string> NextSteps { get; }

    private static List<string> ValidateNonEmptyTextList(IReadOnlyList<string> values, string parameterName)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException($"{parameterName} must contain at least one item.", parameterName);
        }

        var sanitized = new List<string>(values.Count);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            sanitized.Add(value.Trim());
        }

        if (sanitized.Count == 0)
        {
            throw new ArgumentException($"{parameterName} must contain at least one non-empty item.", parameterName);
        }

        return sanitized;
    }
}
