namespace DevOpsAI.Rag.Api.Contracts;

/// <summary>Represents indexed document metadata without exposing its content.</summary>
public sealed record DocumentResponse(
    string DocumentId,
    string FileName,
    string ContentHash,
    int Version,
    string Status,
    int ChunkCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeletedAtUtc);
