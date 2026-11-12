using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Application.Ports;

public interface IRunbookDocumentStore
{
    Task<KnowledgeDocument?> FindByIdAsync(string documentId, CancellationToken cancellationToken);

    /// <summary>Persists an active document snapshot.</summary>
    Task SaveAsync(KnowledgeDocument document, CancellationToken cancellationToken);

    /// <summary>Persists the tombstone and atomically removes its content and searchable data.</summary>
    Task DeleteAsync(KnowledgeDocument tombstone, CancellationToken cancellationToken);
}
