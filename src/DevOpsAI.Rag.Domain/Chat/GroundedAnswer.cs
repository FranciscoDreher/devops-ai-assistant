using System.Collections.Immutable;

namespace DevOpsAI.Rag.Domain.Chat;

public sealed class GroundedAnswer
{
    public GroundedAnswer(
        string text,
        IReadOnlyCollection<RetrievalCandidate> retrievedCandidates,
        IReadOnlyCollection<CitationReference> citations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(retrievedCandidates);
        ArgumentNullException.ThrowIfNull(citations);

        var candidateSnapshot = retrievedCandidates.ToImmutableArray();
        var citationSnapshot = citations.ToImmutableArray();

        if (candidateSnapshot.IsDefaultOrEmpty)
        {
            throw new ArgumentException("A grounded answer requires retrieved evidence.", nameof(retrievedCandidates));
        }

        if (candidateSnapshot.Any(candidate => candidate is null))
        {
            throw new ArgumentException("Retrieved candidates cannot contain null values.", nameof(retrievedCandidates));
        }

        if (citationSnapshot.IsDefaultOrEmpty)
        {
            throw new ArgumentException("A grounded answer requires at least one citation.", nameof(citations));
        }

        if (citationSnapshot.Any(citation => citation is null))
        {
            throw new ArgumentException("Citations cannot contain null values.", nameof(citations));
        }

        var candidateChunks = candidateSnapshot
            .Select(candidate => candidate.Chunk)
            .ToArray();

        if (citationSnapshot.Any(citation => !candidateChunks.Any(citation.References)))
        {
            throw new ArgumentException(
                "Every citation must identify a chunk retrieved for this answer.",
                nameof(citations));
        }

        if (citationSnapshot.Distinct().Count() != citationSnapshot.Length)
        {
            throw new ArgumentException("Citations must not contain duplicates.", nameof(citations));
        }

        Text = text.Trim();
        RetrievedCandidates = candidateSnapshot;
        Citations = citationSnapshot;
    }

    public string Text { get; }

    public ImmutableArray<RetrievalCandidate> RetrievedCandidates { get; }

    public ImmutableArray<CitationReference> Citations { get; }
}
