namespace DevOpsAI.Rag.Application.Documents;

public sealed record ReplaceRunbookDocumentRequest(
    string DocumentId,
    string FileName,
    ReadOnlyMemory<byte> Content);
