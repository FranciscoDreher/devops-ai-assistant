using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.Application.Documents;

public sealed class ReplaceRunbookDocument
{
    private readonly IRunbookDocumentStore _documentStore;
    private readonly IRunbookKnowledgeIndex _knowledgeIndex;
    private readonly RunbookDocumentValidator _validator;
    private readonly TimeProvider _timeProvider;
    private readonly RunbookDocumentChunker _chunker;
    private readonly ITextEmbeddingGenerator _embeddingGenerator;

    public ReplaceRunbookDocument(
        IRunbookDocumentStore documentStore,
        IRunbookKnowledgeIndex knowledgeIndex,
        RunbookDocumentValidator validator,
        TimeProvider timeProvider,
        RunbookDocumentChunker chunker,
        ITextEmbeddingGenerator embeddingGenerator)
    {
        ArgumentNullException.ThrowIfNull(documentStore);
        ArgumentNullException.ThrowIfNull(knowledgeIndex);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(chunker);
        ArgumentNullException.ThrowIfNull(embeddingGenerator);

        _documentStore = documentStore;
        _knowledgeIndex = knowledgeIndex;
        _validator = validator;
        _timeProvider = timeProvider;
        _chunker = chunker;
        _embeddingGenerator = embeddingGenerator;
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
        var embeddings = new List<RunbookChunkEmbedding>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var vector = await _embeddingGenerator
                .GenerateEmbeddingAsync(chunk.Text, cancellationToken)
                .ConfigureAwait(false);
            embeddings.Add(new RunbookChunkEmbedding(chunk.Id, _embeddingGenerator.ModelId, vector));
        }

        await _knowledgeIndex.IndexAsync(document, chunks, embeddings, cancellationToken).ConfigureAwait(false);

        return RunbookDocumentResult.FromDocument(status, RunbookDocumentSummary.From(document, chunks.Count));
    }
}
