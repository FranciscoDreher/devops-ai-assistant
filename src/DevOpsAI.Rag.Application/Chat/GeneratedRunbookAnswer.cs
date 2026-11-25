using System.Collections.Immutable;

namespace DevOpsAI.Rag.Application.Chat;

public sealed record GeneratedRunbookAnswer
{
    public GeneratedRunbookAnswer(
        RunbookAnswerStatus status,
        string answer,
        IReadOnlyCollection<string> citedChunkIds)
    {
        if (status is not RunbookAnswerStatus.Answered and not RunbookAnswerStatus.InsufficientEvidence)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "The answer status is unsupported.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        ArgumentNullException.ThrowIfNull(citedChunkIds);

        var citationSnapshot = citedChunkIds
            .Select(chunkId =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(chunkId);
                return chunkId;
            })
            .ToImmutableArray();

        if (citationSnapshot.Distinct(StringComparer.Ordinal).Count() != citationSnapshot.Length)
        {
            throw new ArgumentException("Cited chunk IDs must not contain duplicates.", nameof(citedChunkIds));
        }

        if (status == RunbookAnswerStatus.Answered && citationSnapshot.IsDefaultOrEmpty)
        {
            throw new ArgumentException("An answered response requires at least one citation.", nameof(citedChunkIds));
        }

        if (status == RunbookAnswerStatus.InsufficientEvidence && !citationSnapshot.IsEmpty)
        {
            throw new ArgumentException("An insufficient-evidence response cannot contain citations.", nameof(citedChunkIds));
        }

        Status = status;
        Answer = answer.Trim();
        CitedChunkIds = citationSnapshot;
    }

    public RunbookAnswerStatus Status { get; }

    public string Answer { get; }

    public ImmutableArray<string> CitedChunkIds { get; }
}
