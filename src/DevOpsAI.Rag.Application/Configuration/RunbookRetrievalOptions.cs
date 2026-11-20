namespace DevOpsAI.Rag.Application.Configuration;

public sealed class RunbookRetrievalOptions
{
    public const string SectionName = "RunbookRetrieval";
    public const int MaximumTopK = 20;

    public double MinimumRelevanceScore { get; init; } = 0.7;

    public int TopK { get; init; } = 5;

    public void Validate()
    {
        if (!double.IsFinite(MinimumRelevanceScore) || MinimumRelevanceScore is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumRelevanceScore),
                "The minimum relevance score must be between zero and one.");
        }

        if (TopK is < 1 or > MaximumTopK)
        {
            throw new ArgumentOutOfRangeException(
                nameof(TopK),
                $"The top-k value must be between one and {MaximumTopK}.");
        }
    }
}
