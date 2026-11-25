namespace DevOpsAI.Rag.Infrastructure.Generation;

public sealed class OllamaChatOptions
{
    public const string SectionName = "Ollama:Chat";

    public string ModelId { get; init; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ModelId))
        {
            throw new InvalidOperationException("An Ollama chat model ID must be configured.");
        }
    }
}
