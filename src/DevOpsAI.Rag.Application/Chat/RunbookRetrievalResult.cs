using DevOpsAI.Rag.Domain.Chat;

namespace DevOpsAI.Rag.Application.Chat;

public sealed record RunbookRetrievalResult
{
    public RunbookRetrievalResult(
        IReadOnlyList<RetrievalCandidate> candidates,
        RunbookEvidenceSufficiency sufficiency)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (sufficiency is not RunbookEvidenceSufficiency.Insufficient
            and not RunbookEvidenceSufficiency.Sufficient)
        {
            throw new ArgumentOutOfRangeException(nameof(sufficiency), sufficiency, "The sufficiency value is unsupported.");
        }

        if ((candidates.Count > 0) != (sufficiency == RunbookEvidenceSufficiency.Sufficient))
        {
            throw new ArgumentException(
                "Sufficiency must indicate whether any candidates passed the relevance threshold.",
                nameof(sufficiency));
        }

        for (var index = 1; index < candidates.Count; index++)
        {
            if (candidates[index - 1].RelevanceScore < candidates[index].RelevanceScore)
            {
                throw new ArgumentException("Retrieval candidates must be ranked by descending relevance.", nameof(candidates));
            }
        }

        Candidates = Array.AsReadOnly(candidates.ToArray());
        Sufficiency = sufficiency;
    }

    public IReadOnlyList<RetrievalCandidate> Candidates { get; }

    public RunbookEvidenceSufficiency Sufficiency { get; }
}
