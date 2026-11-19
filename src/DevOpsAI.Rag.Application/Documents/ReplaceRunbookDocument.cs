using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Application.Documents;

public sealed class ReplaceRunbookDocument
{
    private readonly IRunbookDocumentStore _documentStore;
    private readonly RunbookDocumentValidator _validator;
    private readonly TimeProvider _timeProvider;
    private readonly RunbookDocumentChunker _chunker;

    public ReplaceRunbookDocument(
        IRunbookDocumentStore documentStore,
        RunbookDocumentValidator validator,
        TimeProvider timeProvider,
        RunbookDocumentChunker chunker)
    {
        ArgumentNullException.ThrowIfNull(documentStore);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(chunker);

        _documentStore = documentStore;
        _validator = validator;
        _timeProvider = timeProvider;
        _chunker = chunker;
    }

    public async Task<RunbookDocumentResult> ExecuteAsync(
        ReplaceRunbookDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidatedRunbookDocument validatedDocument;
        try
        {
            validatedDocument = _validator.Validate(request.DocumentId, request.FileName, request.Content);
        }
        catch (RunbookDocumentValidationException exception)
        {
            return RunbookDocumentResult.ForValidationError(exception.ErrorCode);
        }

        var existingDocument = await _documentStore.FindByIdAsync(
            validatedDocument.Id,
            cancellationToken).ConfigureAwait(false);

        var updatedAtUtc = _timeProvider.GetUtcNow();
        KnowledgeDocument document;
        RunbookDocumentOperationStatus status;

        if (existingDocument is null)
        {
            document = KnowledgeDocument.Create(
                validatedDocument.Id,
                validatedDocument.FileName,
                validatedDocument.Content,
                updatedAtUtc);
            status = RunbookDocumentOperationStatus.Created;
        }
        else
        {
            if (updatedAtUtc < existingDocument.UpdatedAtUtc)
            {
                updatedAtUtc = existingDocument.UpdatedAtUtc;
            }

            document = existingDocument.Replace(
                validatedDocument.FileName,
                validatedDocument.Content,
                updatedAtUtc);
            status = RunbookDocumentOperationStatus.Replaced;
        }

        var chunks = _chunker.Chunk(document);
        await _documentStore.SaveAsync(document, chunks, cancellationToken).ConfigureAwait(false);

        return RunbookDocumentResult.FromDocument(status, RunbookDocumentSummary.From(document));
    }
}
