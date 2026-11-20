namespace DevOpsAI.Rag.Infrastructure.Embeddings;

public sealed class OllamaEmbeddingOptions
{
    public const string SectionName = "Ollama";

    public string Endpoint { get; init; } = "http://127.0.0.1:11434";

    public string ModelId { get; init; } = string.Empty;

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(60);

    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme is not "http" and not "https")
        {
            throw new InvalidOperationException("The Ollama endpoint must be an absolute HTTP or HTTPS URI.");
        }

        if (string.IsNullOrWhiteSpace(ModelId))
        {
            throw new InvalidOperationException("An Ollama embedding model ID must be configured.");
        }

        if (RequestTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("The Ollama request timeout must be greater than zero.");
        }
    }
}
