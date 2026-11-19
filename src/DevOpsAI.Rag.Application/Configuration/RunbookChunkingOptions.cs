namespace DevOpsAI.Rag.Application.Configuration;

public sealed class RunbookChunkingOptions
{
    public const string SectionName = "RunbookChunking";

    public int MaximumChunkCharacters { get; init; } = 1_000;

    public int OverlapCharacters { get; init; } = 150;
}
