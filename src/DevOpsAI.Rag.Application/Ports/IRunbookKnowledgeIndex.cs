using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Application.Ports;

public interface IRunbookKnowledgeIndex
{
    Task IndexAsync(
        KnowledgeDocument document,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<RunbookChunkEmbedding> embeddings,
        CancellationToken cancellationToken);

    Task<RunbookRetrievalResult> SearchAsync(
        string embeddingModelId,
        ReadOnlyMemory<float> queryEmbedding,
        CancellationToken cancellationToken);
}
