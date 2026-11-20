using DevOpsAI.Rag.Infrastructure.Embeddings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DevOpsAI.Rag.UnitTests.Embeddings;

public sealed class OllamaTextEmbeddingGeneratorTests
{
    private static readonly float[] ValidVector = [0.6f, 0.8f];
    private static readonly float[] ZeroVector = [0f, 0f];
    private static readonly OllamaEmbeddingOptions Options = new()
    {
        ModelId = "local-test-model"
    };

    [Fact]
    public async Task GenerateEmbeddingAsyncReturnsTheProviderVectorAndModelId()
    {
        var provider = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        provider.GenerateAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<EmbeddingGenerationOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
                [new Embedding<float>(ValidVector)])));
        var generator = new OllamaTextEmbeddingGenerator(provider, Microsoft.Extensions.Options.Options.Create(Options));

        var vector = await generator.GenerateEmbeddingAsync("recover cache", CancellationToken.None);

        Assert.Equal("local-test-model", generator.ModelId);
        Assert.Equal(ValidVector, vector.ToArray());
        await provider.Received(1).GenerateAsync(
            Arg.Is<IEnumerable<string>>(inputs => inputs.Single() == "recover cache"),
            Arg.Any<EmbeddingGenerationOptions?>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task GenerateEmbeddingAsyncRejectsAnInvalidProviderVector()
    {
        var provider = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        provider.GenerateAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<EmbeddingGenerationOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
                [new Embedding<float>(ZeroVector)])));
        var generator = new OllamaTextEmbeddingGenerator(provider, Microsoft.Extensions.Options.Options.Create(Options));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => generator.GenerateEmbeddingAsync("recover cache", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateEmbeddingAsyncPropagatesProviderFailures()
    {
        var provider = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        provider.GenerateAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<EmbeddingGenerationOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GeneratedEmbeddings<Embedding<float>>>(
                new HttpRequestException("Ollama unavailable.")));
        var generator = new OllamaTextEmbeddingGenerator(provider, Microsoft.Extensions.Options.Options.Create(Options));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateEmbeddingAsync("recover cache", CancellationToken.None));
    }

    [Fact]
    public void ConstructorRejectsMissingEmbeddingModelConfiguration()
    {
        var provider = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        var invalidOptions = Microsoft.Extensions.Options.Options.Create(new OllamaEmbeddingOptions());

        Assert.Throws<InvalidOperationException>(
            () => new OllamaTextEmbeddingGenerator(provider, invalidOptions));
    }
}
