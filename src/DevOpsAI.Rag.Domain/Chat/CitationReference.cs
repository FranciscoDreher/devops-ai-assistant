using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Domain.Chat;

public sealed record CitationReference
{
    public CitationReference(DocumentChunk chunk)
        : this(
            chunk?.Document.Id ?? throw new ArgumentNullException(nameof(chunk)),
            chunk.Document.Version,
            chunk.Id,
            chunk.Heading,
            chunk.StartLine,
            chunk.EndLine)
    {
    }

    public CitationReference(
        string documentId,
        int documentVersion,
        string chunkId,
        string? heading,
        int startLine,
        int endLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chunkId);

        if (documentVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(documentVersion), "The document version must be positive.");
        }

        if (startLine < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startLine), "The starting source line must be positive.");
        }

        if (endLine < startLine)
        {
            throw new ArgumentOutOfRangeException(nameof(endLine), "The ending source line cannot precede the start.");
        }

        DocumentId = documentId.Trim();
        DocumentVersion = documentVersion;
        ChunkId = chunkId.Trim();
        Heading = string.IsNullOrWhiteSpace(heading) ? null : heading.Trim();
        StartLine = startLine;
        EndLine = endLine;
    }

    public string DocumentId { get; }

    public int DocumentVersion { get; }

    public string ChunkId { get; }

    public string? Heading { get; }

    public int StartLine { get; }

    public int EndLine { get; }

    internal bool References(DocumentChunk chunk)
    {
        return string.Equals(DocumentId, chunk.Document.Id, StringComparison.Ordinal)
            && DocumentVersion == chunk.Document.Version
            && string.Equals(ChunkId, chunk.Id, StringComparison.Ordinal)
            && string.Equals(Heading, chunk.Heading, StringComparison.Ordinal)
            && StartLine == chunk.StartLine
            && EndLine == chunk.EndLine;
    }
}
