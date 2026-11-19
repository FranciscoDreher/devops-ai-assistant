using DevOpsAI.Rag.Application.Documents;
using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Application.Ports;

public interface IRunbookDocumentStore
{
    Task<KnowledgeDocument?> FindByIdAsync(string documentId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists an active snapshot. Replacing an existing ID must atomically invalidate all searchable data
    /// derived from its previous version so queries cannot observe stale content.
    /// </summary>
    Task SaveAsync(KnowledgeDocument document, CancellationToken cancellationToken);

    /// <summary>Persists the tombstone and atomically removes its content and searchable data.</summary>
    Task DeleteAsync(KnowledgeDocument tombstone, CancellationToken cancellationToken);

    /// <summary>Returns document metadata, including deletion tombstones, without loading document content.</summary>
    Task<IReadOnlyList<RunbookDocumentSummary>> ListAsync(CancellationToken cancellationToken);
}
