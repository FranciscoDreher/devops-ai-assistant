namespace DevOpsAI.Rag.Application.Documents;

public sealed record RunbookDocumentResult
{
    private RunbookDocumentResult(
        RunbookDocumentOperationStatus status,
        RunbookDocumentSummary? document,
        RunbookDocumentValidationErrorCode? validationError)
    {
        Status = status;
        Document = document;
        ValidationError = validationError;
    }

    public RunbookDocumentOperationStatus Status { get; }

    public RunbookDocumentSummary? Document { get; }

    public RunbookDocumentValidationErrorCode? ValidationError { get; }

    public static RunbookDocumentResult FromDocument(
        RunbookDocumentOperationStatus status,
        RunbookDocumentSummary document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (status is not RunbookDocumentOperationStatus.Created
            and not RunbookDocumentOperationStatus.Replaced
            and not RunbookDocumentOperationStatus.Deleted)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "The operation status is not a document result.");
        }

        return new RunbookDocumentResult(status, document, validationError: null);
    }

    public static RunbookDocumentResult ForValidationError(RunbookDocumentValidationErrorCode errorCode)
    {
        return new RunbookDocumentResult(
            RunbookDocumentOperationStatus.ValidationError,
            document: null,
            errorCode);
    }

    public static RunbookDocumentResult NotFound()
    {
        return new RunbookDocumentResult(
            RunbookDocumentOperationStatus.NotFound,
            document: null,
            validationError: null);
    }
}
