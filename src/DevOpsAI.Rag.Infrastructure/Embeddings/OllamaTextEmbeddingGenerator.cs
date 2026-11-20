using DevOpsAI.Rag.Application.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.Infrastructure.Embeddings;

public sealed class OllamaTextEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public OllamaTextEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<OllamaEmbeddingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(embeddingGenerator);
        ArgumentNullException.ThrowIfNull(options);
        options.Value.Validate();

        _embeddingGenerator = embeddingGenerator;
        ModelId = options.Value.ModelId.Trim();
    }

    public string ModelId { get; }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var embeddings = await _embeddingGenerator
            .GenerateAsync([text], cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (embeddings.Count != 1)
        {
            throw new InvalidDataException("The embedding provider returned an unexpected number of vectors.");
        }

        var vector = embeddings[0].Vector;
        if (vector.IsEmpty)
        {
            throw new InvalidDataException("The embedding provider returned an invalid vector.");
        }

        var hasNonZeroValue = false;
        foreach (var value in vector.Span)
        {
            if (!float.IsFinite(value))
            {
                throw new InvalidDataException("The embedding provider returned an invalid vector.");
            }

            hasNonZeroValue |= value != 0;
        }

        if (!hasNonZeroValue)
        {
            throw new InvalidDataException("The embedding provider returned an invalid vector.");
        }

        return new ReadOnlyMemory<float>(vector.ToArray());
    }
}
