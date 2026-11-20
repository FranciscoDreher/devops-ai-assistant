namespace DevOpsAI.Rag.Application.Ports;

public interface ITextEmbeddingGenerator
{
    string ModelId { get; }

    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken);
}
