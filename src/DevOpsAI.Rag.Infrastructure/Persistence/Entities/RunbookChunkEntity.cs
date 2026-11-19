namespace DevOpsAI.Rag.Infrastructure.Persistence.Entities;

public sealed class RunbookChunkEntity
{
    public string Id { get; set; } = string.Empty;

    public string DocumentId { get; set; } = string.Empty;

    public int DocumentVersion { get; set; }

    public int Ordinal { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? Heading { get; set; }

    public int StartLine { get; set; }

    public int EndLine { get; set; }

    public RunbookDocumentEntity Document { get; set; } = null!;

    public RunbookCitationReferenceEntity? CitationReference { get; set; }
}
