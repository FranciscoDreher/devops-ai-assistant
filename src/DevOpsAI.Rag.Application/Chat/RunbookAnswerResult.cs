using System.Collections.Immutable;
using DevOpsAI.Rag.Domain.Chat;

namespace DevOpsAI.Rag.Application.Chat;

public sealed record RunbookAnswerResult
{
    private RunbookAnswerResult(
        RunbookAnswerStatus status,
        string answer,
        IReadOnlyCollection<CitationReference> citations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        ArgumentNullException.ThrowIfNull(citations);

        var citationSnapshot = citations.ToImmutableArray();
        if (status == RunbookAnswerStatus.Answered && citationSnapshot.IsDefaultOrEmpty)
        {
            throw new ArgumentException("An answered result requires at least one citation.", nameof(citations));
        }

        if (status == RunbookAnswerStatus.InsufficientEvidence && !citationSnapshot.IsEmpty)
        {
            throw new ArgumentException("An insufficient-evidence result cannot contain citations.", nameof(citations));
        }

        Status = status;
        Answer = answer.Trim();
        Citations = citationSnapshot;
    }

    public RunbookAnswerStatus Status { get; }

    public string Answer { get; }

    public ImmutableArray<CitationReference> Citations { get; }

    public static RunbookAnswerResult Answered(GroundedAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);
        return new RunbookAnswerResult(RunbookAnswerStatus.Answered, answer.Text, answer.Citations);
    }

    public static RunbookAnswerResult InsufficientEvidence(string answer)
    {
        return new RunbookAnswerResult(RunbookAnswerStatus.InsufficientEvidence, answer, []);
    }
}
