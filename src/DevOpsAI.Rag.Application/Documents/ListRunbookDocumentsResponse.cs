namespace DevOpsAI.Rag.Application.Documents;

public sealed record ListRunbookDocumentsResponse(IReadOnlyList<RunbookDocumentSummary> Documents);
