namespace DevOpsAI.Rag.Application.Ports;

public sealed record RunbookChunkEmbedding
{
    public RunbookChunkEmbedding(string chunkId, string modelId, ReadOnlyMemory<float> vector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chunkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        if (vector.IsEmpty)
        {
            throw new ArgumentException("The embedding vector must contain finite values.", nameof(vector));
        }

        var hasNonZeroValue = false;
        foreach (var value in vector.Span)
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("The embedding vector must contain finite values.", nameof(vector));
            }

            hasNonZeroValue |= value != 0;
        }

        if (!hasNonZeroValue)
        {
            throw new ArgumentException("The embedding vector cannot be zero.", nameof(vector));
        }

        ChunkId = chunkId.Trim();
        ModelId = modelId.Trim();
        Vector = new ReadOnlyMemory<float>(vector.ToArray());
    }

    public string ChunkId { get; }

    public string ModelId { get; }

    public ReadOnlyMemory<float> Vector { get; }
}
