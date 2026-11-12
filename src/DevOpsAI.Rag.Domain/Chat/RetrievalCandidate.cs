using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Domain.Chat;

public sealed record RetrievalCandidate
{
    public RetrievalCandidate(DocumentChunk chunk, double relevanceScore)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        if (!double.IsFinite(relevanceScore) || relevanceScore is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(relevanceScore),
                "The relevance score must be a finite value between zero and one.");
        }

        if (chunk.Document.Status != KnowledgeDocumentStatus.Active)
        {
            throw new ArgumentException("Deleted document chunks cannot be retrieval candidates.", nameof(chunk));
        }

        Chunk = chunk;
        RelevanceScore = relevanceScore;
    }

    public DocumentChunk Chunk { get; }

    public double RelevanceScore { get; }
}
