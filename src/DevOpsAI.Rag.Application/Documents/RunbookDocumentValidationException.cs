namespace DevOpsAI.Rag.Application.Documents;

public sealed class RunbookDocumentValidationException : Exception
{
    public RunbookDocumentValidationException(RunbookDocumentValidationErrorCode errorCode)
        : base($"Document validation failed: {errorCode}.")
    {
        ErrorCode = errorCode;
    }

    public RunbookDocumentValidationErrorCode ErrorCode { get; }
}
