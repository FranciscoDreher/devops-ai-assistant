namespace DevOpsAI.Rag.Application.Configuration;

public sealed class RunbookDocumentOptions
{
    public const string SectionName = "RunbookDocuments";

    public long MaxDocumentSizeBytes { get; init; } = 1_048_576;
}
