using System.Collections.Immutable;

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

        ActionsTaken = ValidateNonEmptyTextList(actionsTaken, nameof(actionsTaken)).ToImmutableArray();
        NextSteps = ValidateNonEmptyTextList(nextSteps, nameof(nextSteps)).ToImmutableArray();
    }

    public string Summary { get; }

    public string Impact { get; }

    public ImmutableArray<string> ActionsTaken { get; }

    public string CurrentStatus { get; }

    public ImmutableArray<string> NextSteps { get; }

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
                throw new ArgumentException($"{parameterName} contains empty or whitespace entries.", parameterName);
            }

            var trimmed = value.Trim();
            sanitized.Add(trimmed);
        }

        if (sanitized.Count == 0)
        {
            throw new ArgumentException($"{parameterName} must contain at least one non-empty item.", parameterName);
        }

        return sanitized;
    }
}
