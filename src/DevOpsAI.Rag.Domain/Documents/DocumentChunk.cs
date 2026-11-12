namespace DevOpsAI.Rag.Domain.Documents;

public sealed record DocumentChunk
{
    public DocumentChunk(
        string id,
        KnowledgeDocument document,
        int ordinal,
        string text,
        string? heading,
        int startLine,
        int endLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (ordinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), "The chunk ordinal cannot be negative.");
        }

        if (startLine < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startLine), "The starting source line must be positive.");
        }

        if (endLine < startLine)
        {
            throw new ArgumentOutOfRangeException(nameof(endLine), "The ending source line cannot precede the start.");
        }

        Id = id.Trim();
        Document = document;
        Ordinal = ordinal;
        Text = text;
        Heading = string.IsNullOrWhiteSpace(heading) ? null : heading.Trim();
        StartLine = startLine;
        EndLine = endLine;
    }

    public string Id { get; }

    public KnowledgeDocument Document { get; }

    public int Ordinal { get; }

    public string Text { get; }

    public string? Heading { get; }

    public int StartLine { get; }

    public int EndLine { get; }
}
