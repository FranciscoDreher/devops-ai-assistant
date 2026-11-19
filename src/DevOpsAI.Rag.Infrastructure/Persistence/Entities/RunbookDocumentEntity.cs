using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Infrastructure.Persistence.Entities;

public sealed class RunbookDocumentEntity
{
    public string Id { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? Content { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public int Version { get; set; }

    public KnowledgeDocumentStatus Status { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public ICollection<RunbookChunkEntity> Chunks { get; set; } = new List<RunbookChunkEntity>();
}
