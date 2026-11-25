using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Chat;
using DevOpsAI.Rag.Domain.Documents;
using NSubstitute;

namespace DevOpsAI.Rag.ApplicationTests.Chat;

public sealed class GenerateRunbookAnswerTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsyncWhenEvidenceIsInsufficientAbstainsWithoutLoadingPromptOrGenerating()
    {
        var promptProvider = Substitute.For<IRunbookPromptProvider>();
        var generator = Substitute.For<IRunbookAnswerGenerator>();
        var useCase = new GenerateRunbookAnswer(promptProvider, generator);
        var retrieval = new RunbookRetrievalResult([], RunbookEvidenceSufficiency.Insufficient);

        var result = await useCase.ExecuteAsync("How do I restore service?", retrieval, CancellationToken.None);

        Assert.Equal(RunbookAnswerStatus.InsufficientEvidence, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.Answer));
        Assert.Empty(result.Citations);
        await promptProvider.DidNotReceive()
            .GetPromptAsync(Arg.Any<CancellationToken>());
        await generator.DidNotReceive()
            .GenerateAsync(Arg.Any<RunbookAnswerPrompt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsyncWhenGeneratorReturnsRetrievedCitationUsesTrustedChunkMetadata()
    {
        var candidate = CreateCandidate("chunk-recovery", "Restart the cache service.", 0.94);
        var promptProvider = Substitute.For<IRunbookPromptProvider>();
        promptProvider.GetPromptAsync(Arg.Any<CancellationToken>())
            .Returns(new RunbookPromptInstructions("Ground answers.", "Return the required JSON."));
        var generator = Substitute.For<IRunbookAnswerGenerator>();
        generator.GenerateAsync(Arg.Any<RunbookAnswerPrompt>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedRunbookAnswer(
                RunbookAnswerStatus.Answered,
                "Restart the cache service.",
                ["chunk-recovery"]));
        var useCase = new GenerateRunbookAnswer(promptProvider, generator);
        var retrieval = new RunbookRetrievalResult([candidate], RunbookEvidenceSufficiency.Sufficient);

        var result = await useCase.ExecuteAsync("How do I recover the cache?", retrieval, CancellationToken.None);

        Assert.Equal(RunbookAnswerStatus.Answered, result.Status);
        Assert.Equal("Restart the cache service.", result.Answer);
        var citation = Assert.Single(result.Citations);
        Assert.Equal(candidate.Chunk.Document.Id, citation.DocumentId);
        Assert.Equal(candidate.Chunk.Document.Version, citation.DocumentVersion);
        Assert.Equal(candidate.Chunk.Id, citation.ChunkId);
        Assert.Equal(candidate.Chunk.Heading, citation.Heading);
        Assert.Equal(candidate.Chunk.StartLine, citation.StartLine);
        Assert.Equal(candidate.Chunk.EndLine, citation.EndLine);

        await generator.Received(1)
            .GenerateAsync(
                Arg.Is<RunbookAnswerPrompt>((RunbookAnswerPrompt prompt) =>
                    prompt.Question == "How do I recover the cache?"
                    && prompt.Evidence.Length == 1
                    && prompt.Evidence[0].ChunkId == candidate.Chunk.Id
                    && prompt.Evidence[0].Text == candidate.Chunk.Text),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsyncWhenGeneratorCitesUnretrievedChunkRejectsTheAnswer()
    {
        var candidate = CreateCandidate("chunk-recovery", "Restart the cache service.", 0.94);
        var promptProvider = Substitute.For<IRunbookPromptProvider>();
        promptProvider.GetPromptAsync(Arg.Any<CancellationToken>())
            .Returns(new RunbookPromptInstructions("Ground answers.", "Return the required JSON."));
        var generator = Substitute.For<IRunbookAnswerGenerator>();
        generator.GenerateAsync(Arg.Any<RunbookAnswerPrompt>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedRunbookAnswer(
                RunbookAnswerStatus.Answered,
                "Restart the cache service.",
                ["invented-chunk"]));
        var useCase = new GenerateRunbookAnswer(promptProvider, generator);
        var retrieval = new RunbookRetrievalResult([candidate], RunbookEvidenceSufficiency.Sufficient);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            useCase.ExecuteAsync("How do I recover the cache?", retrieval, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsyncWhenGeneratorAbstainsReturnsInsufficientEvidenceWithoutCitations()
    {
        var candidate = CreateCandidate("chunk-recovery", "Restart the cache service.", 0.94);
        var promptProvider = Substitute.For<IRunbookPromptProvider>();
        promptProvider.GetPromptAsync(Arg.Any<CancellationToken>())
            .Returns(new RunbookPromptInstructions("Ground answers.", "Return the required JSON."));
        var generator = Substitute.For<IRunbookAnswerGenerator>();
        generator.GenerateAsync(Arg.Any<RunbookAnswerPrompt>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedRunbookAnswer(
                RunbookAnswerStatus.InsufficientEvidence,
                "The retrieved passages do not contain enough information to answer.",
                []));
        var useCase = new GenerateRunbookAnswer(promptProvider, generator);
        var retrieval = new RunbookRetrievalResult([candidate], RunbookEvidenceSufficiency.Sufficient);

        var result = await useCase.ExecuteAsync("How do I recover the cache?", retrieval, CancellationToken.None);

        Assert.Equal(RunbookAnswerStatus.InsufficientEvidence, result.Status);
        Assert.Contains("enough information", result.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Citations);
    }

    [Fact]
    public async Task ExecuteAsyncWhenQuestionIsBlankRejectsInputBeforeCallingDependencies()
    {
        var promptProvider = Substitute.For<IRunbookPromptProvider>();
        var generator = Substitute.For<IRunbookAnswerGenerator>();
        var useCase = new GenerateRunbookAnswer(promptProvider, generator);
        var retrieval = new RunbookRetrievalResult([], RunbookEvidenceSufficiency.Insufficient);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(" ", retrieval, CancellationToken.None));

        await promptProvider.DidNotReceive()
            .GetPromptAsync(Arg.Any<CancellationToken>());
        await generator.DidNotReceive()
            .GenerateAsync(Arg.Any<RunbookAnswerPrompt>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsyncWhenCancelledStopsBeforeLoadingPromptOrGenerating()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var promptProvider = Substitute.For<IRunbookPromptProvider>();
        var generator = Substitute.For<IRunbookAnswerGenerator>();
        var useCase = new GenerateRunbookAnswer(promptProvider, generator);
        var candidate = CreateCandidate("chunk-recovery", "Restart the cache service.", 0.94);
        var retrieval = new RunbookRetrievalResult([candidate], RunbookEvidenceSufficiency.Sufficient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync("How do I recover the cache?", retrieval, cancellationSource.Token));

        await promptProvider.DidNotReceive()
            .GetPromptAsync(Arg.Any<CancellationToken>());
        await generator.DidNotReceive()
            .GenerateAsync(Arg.Any<RunbookAnswerPrompt>(), Arg.Any<CancellationToken>());
    }

    private static RetrievalCandidate CreateCandidate(string chunkId, string text, double score)
    {
        var document = KnowledgeDocument.Create(
            "cache-recovery",
            "cache-recovery.md",
            $"# Recovery\n{text}",
            CreatedAtUtc);
        var chunk = new DocumentChunk(chunkId, document, 0, text, "Recovery", 2, 2);
        return new RetrievalCandidate(chunk, score);
    }
}
