using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Application.Documents;

public sealed record RunbookDocumentSummary(
    string DocumentId,
    string FileName,
    string ContentHash,
    int Version,
    KnowledgeDocumentStatus Status,
    int ChunkCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeletedAtUtc)
{
    public static RunbookDocumentSummary From(KnowledgeDocument document, int chunkCount)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfNegative(chunkCount);

        return new RunbookDocumentSummary(
            document.Id,
            document.FileName,
            document.ContentHash,
            document.Version,
            document.Status,
            chunkCount,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.DeletedAtUtc);
    }
}
