namespace DevOpsAI.Rag.Infrastructure.Persistence.Entities;

public sealed class RunbookCitationReferenceEntity
{
    public string ChunkId { get; set; } = string.Empty;

    public string DocumentId { get; set; } = string.Empty;

    public int DocumentVersion { get; set; }

    public string? Heading { get; set; }

    public int StartLine { get; set; }

    public int EndLine { get; set; }

    public RunbookChunkEntity Chunk { get; set; } = null!;
}
