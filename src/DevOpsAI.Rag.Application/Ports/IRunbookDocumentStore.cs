using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Application.Ports;

public interface IRunbookDocumentStore
{
    Task<KnowledgeDocument?> FindByIdAsync(string documentId, CancellationToken cancellationToken);

    Task SaveAsync(KnowledgeDocument document, CancellationToken cancellationToken);
}
