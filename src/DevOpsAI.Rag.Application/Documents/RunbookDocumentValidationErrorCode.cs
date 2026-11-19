namespace DevOpsAI.Rag.Application.Documents;

public enum RunbookDocumentValidationErrorCode
{
    InvalidDocumentId,
    InvalidFileName,
    UnsupportedExtension,
    DocumentTooLarge,
    InvalidUtf8,
    EmptyContent
}
