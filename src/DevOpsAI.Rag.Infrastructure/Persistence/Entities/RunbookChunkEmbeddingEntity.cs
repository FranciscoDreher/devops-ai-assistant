namespace DevOpsAI.Rag.Infrastructure.Persistence.Entities;

public sealed class RunbookChunkEmbeddingEntity
{
    public string ChunkId { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public int Dimensions { get; set; }

    public byte[] VectorBlob { get; set; } = [];

    public RunbookChunkEntity Chunk { get; set; } = null!;
}
