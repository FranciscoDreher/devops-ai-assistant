using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Application.Documents;

public sealed class DeleteRunbookDocument
{
    private readonly IRunbookDocumentStore _documentStore;
    private readonly TimeProvider _timeProvider;

    public DeleteRunbookDocument(IRunbookDocumentStore documentStore, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(documentStore);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _documentStore = documentStore;
        _timeProvider = timeProvider;
    }

    public async Task<RunbookDocumentResult> ExecuteAsync(
        DeleteRunbookDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string documentId;
        try
        {
            documentId = KnowledgeDocument.NormalizeId(request.DocumentId);
        }
        catch (ArgumentException)
        {
            return RunbookDocumentResult.ForValidationError(
                RunbookDocumentValidationErrorCode.InvalidDocumentId);
        }

        var document = await _documentStore.FindByIdAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (document is null || document.Status == KnowledgeDocumentStatus.Deleted)
        {
            return RunbookDocumentResult.NotFound();
        }

        var deletedAtUtc = _timeProvider.GetUtcNow();
        if (deletedAtUtc < document.UpdatedAtUtc)
        {
            deletedAtUtc = document.UpdatedAtUtc;
        }

        var tombstone = document.Delete(deletedAtUtc);
        await _documentStore.DeleteAsync(tombstone, cancellationToken).ConfigureAwait(false);

        return RunbookDocumentResult.FromDocument(
            RunbookDocumentOperationStatus.Deleted,
            RunbookDocumentSummary.From(tombstone, chunkCount: 0));
    }
}
