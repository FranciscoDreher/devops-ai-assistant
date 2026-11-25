namespace DevOpsAI.Rag.Api.Contracts;

/// <summary>Contains the document metadata currently known to the local corpus.</summary>
public sealed record DocumentListResponse(IReadOnlyList<DocumentResponse> Documents);
