using System.Security.Cryptography;
using System.Text;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Documents;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Documents;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DevOpsAI.Rag.ApplicationTests.Documents;

public class RunbookDocumentLifecycleTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly float[] TestEmbedding = [1f, 0f];

    [Fact]
    public async Task ReplaceAsyncForMissingDocumentSavesCreatedSnapshot()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        store.FindByIdAsync("runbook", Arg.Any<CancellationToken>())
            .Returns((KnowledgeDocument?)null);
        var useCase = CreateReplaceUseCase(store);

        var result = await useCase.ExecuteAsync(
            CreateReplaceRequest("runbook", "cache.md", "Restart cache"),
            CancellationToken.None);

        Assert.Equal(RunbookDocumentOperationStatus.Created, result.Status);
        Assert.NotNull(result.Document);
        Assert.Equal(1, result.Document.Version);
        Assert.Equal(
            $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("Restart cache"))).ToLowerInvariant()}",
            result.Document.ContentHash);
        await store.Received(1).SaveAsync(
            Arg.Is<KnowledgeDocument>(document =>
                document.Id == "runbook"
                && document.FileName == "cache.md"
                && document.Content == "Restart cache"
                && document.Version == 1),
            Arg.Is<IReadOnlyList<DocumentChunk>>(chunks =>
                chunks.Count == 1
                && chunks[0].Text == "Restart cache"
                && chunks[0].StartLine == 1
                && chunks[0].EndLine == 1),
            Arg.Is<IReadOnlyList<RunbookChunkEmbedding>>(embeddings =>
                embeddings.Count == 1
                && embeddings[0].ChunkId == "runbook:v1:chunk:0"
                && embeddings[0].ModelId == "test-embedding-model"
                && embeddings[0].Vector.Length == 2
                && embeddings[0].Vector.ToArray()[0] == 1f
                && embeddings[0].Vector.ToArray()[1] == 0f),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceAsyncForExistingDocumentReplacesVersionAndSnapshot()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        var existing = KnowledgeDocument.Create(
            "runbook",
            "old.md",
            "Old content",
            CurrentTime.AddDays(-1));
        store.FindByIdAsync("runbook", Arg.Any<CancellationToken>())
            .Returns(existing);
        var useCase = CreateReplaceUseCase(store);

        var result = await useCase.ExecuteAsync(
            CreateReplaceRequest("runbook", "new.md", "New content"),
            CancellationToken.None);

        Assert.Equal(RunbookDocumentOperationStatus.Replaced, result.Status);
        Assert.NotNull(result.Document);
        Assert.Equal(2, result.Document.Version);
        Assert.Equal("new.md", result.Document.FileName);
        Assert.NotEqual(existing.ContentHash, result.Document.ContentHash);
        await store.Received(1).SaveAsync(
            Arg.Is<KnowledgeDocument>(document =>
                document.Version == 2
                && document.Content == "New content"
                && document.Status == KnowledgeDocumentStatus.Active),
            Arg.Is<IReadOnlyList<DocumentChunk>>(chunks =>
                chunks.Count == 1
                && chunks[0].Document.Version == 2
                && chunks[0].Text == "New content"),
            Arg.Is<IReadOnlyList<RunbookChunkEmbedding>>(embeddings =>
                embeddings.Count == 1
                && embeddings[0].ChunkId == "runbook:v2:chunk:0"
                && embeddings[0].ModelId == "test-embedding-model"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceAsyncForInvalidDocumentReturnsValidationErrorWithoutStoreAccess()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        var useCase = CreateReplaceUseCase(store);

        var result = await useCase.ExecuteAsync(
            CreateReplaceRequest("runbook", "runbook.pdf", "content"),
            CancellationToken.None);

        Assert.Equal(RunbookDocumentOperationStatus.ValidationError, result.Status);
        Assert.Equal(
            RunbookDocumentValidationErrorCode.UnsupportedExtension,
            result.ValidationError);
        await store.DidNotReceive()
            .FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive()
            .SaveAsync(Arg.Any<KnowledgeDocument>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive()
            .SaveAsync(
                Arg.Any<KnowledgeDocument>(),
                Arg.Any<IReadOnlyList<DocumentChunk>>(),
                Arg.Any<CancellationToken>());
        await store.DidNotReceive()
            .SaveAsync(
                Arg.Any<KnowledgeDocument>(),
                Arg.Any<IReadOnlyList<DocumentChunk>>(),
                Arg.Any<IReadOnlyList<RunbookChunkEmbedding>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceAsyncPropagatesStoreFailure()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        store.FindByIdAsync("runbook", Arg.Any<CancellationToken>())
            .Returns<KnowledgeDocument?>(_ => throw new InvalidOperationException("Storage unavailable."));
        var useCase = CreateReplaceUseCase(store);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(
                CreateReplaceRequest("runbook", "runbook.md", "content"),
                CancellationToken.None));

        await store.DidNotReceive()
            .SaveAsync(Arg.Any<KnowledgeDocument>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive()
            .SaveAsync(
                Arg.Any<KnowledgeDocument>(),
                Arg.Any<IReadOnlyList<DocumentChunk>>(),
                Arg.Any<CancellationToken>());
        await store.DidNotReceive()
            .SaveAsync(
                Arg.Any<KnowledgeDocument>(),
                Arg.Any<IReadOnlyList<DocumentChunk>>(),
                Arg.Any<IReadOnlyList<RunbookChunkEmbedding>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceAsyncPropagatesSaveFailureAndCancellationToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var store = Substitute.For<IRunbookDocumentStore>();
        store.FindByIdAsync("runbook", cancellationToken)
            .Returns((KnowledgeDocument?)null);
        store.SaveAsync(
                Arg.Any<KnowledgeDocument>(),
                Arg.Any<IReadOnlyList<DocumentChunk>>(),
                Arg.Any<IReadOnlyList<RunbookChunkEmbedding>>(),
                cancellationToken)
            .Returns<Task>(_ => throw new InvalidOperationException("Storage unavailable."));
        var useCase = CreateReplaceUseCase(store);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(
                CreateReplaceRequest("runbook", "runbook.md", "content"),
                cancellationToken));

        await store.Received(1)
            .SaveAsync(
                Arg.Any<KnowledgeDocument>(),
                Arg.Any<IReadOnlyList<DocumentChunk>>(),
                Arg.Any<IReadOnlyList<RunbookChunkEmbedding>>(),
                cancellationToken);
    }

    [Fact]
    public async Task ReplaceAsyncPropagatesEmbeddingFailureWithoutSavingDocument()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        store.FindByIdAsync("runbook", Arg.Any<CancellationToken>())
            .Returns((KnowledgeDocument?)null);
        var embeddingGenerator = Substitute.For<ITextEmbeddingGenerator>();
        embeddingGenerator.ModelId.Returns("test-embedding-model");
        embeddingGenerator.GenerateEmbeddingAsync("Content", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<ReadOnlyMemory<float>>(
                new InvalidOperationException("Embedding provider unavailable.")));
        var useCase = CreateReplaceUseCase(store, embeddingGenerator);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(
                CreateReplaceRequest("runbook", "runbook.md", "Content"),
                CancellationToken.None));

        await store.DidNotReceive()
            .SaveAsync(
                Arg.Any<KnowledgeDocument>(),
                Arg.Any<IReadOnlyList<DocumentChunk>>(),
                Arg.Any<IReadOnlyList<RunbookChunkEmbedding>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceAsyncPropagatesCancellationToEmbeddingGeneration()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var store = Substitute.For<IRunbookDocumentStore>();
        store.FindByIdAsync("runbook", cancellationToken)
            .Returns((KnowledgeDocument?)null);
        var embeddingGenerator = Substitute.For<ITextEmbeddingGenerator>();
        embeddingGenerator.ModelId.Returns("test-embedding-model");
        embeddingGenerator.GenerateEmbeddingAsync("Content", cancellationToken)
            .Returns(Task.FromResult<ReadOnlyMemory<float>>(TestEmbedding));
        var useCase = CreateReplaceUseCase(store, embeddingGenerator);

        await useCase.ExecuteAsync(
            CreateReplaceRequest("runbook", "runbook.md", "Content"),
            cancellationToken);

        await embeddingGenerator.Received(1)
            .GenerateEmbeddingAsync("Content", cancellationToken);
    }

    [Fact]
    public async Task DeleteAsyncForExistingDocumentDeletesTombstone()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        var existing = KnowledgeDocument.Create(
            "runbook",
            "runbook.md",
            "Content",
            CurrentTime.AddDays(-1));
        store.FindByIdAsync("runbook", Arg.Any<CancellationToken>())
            .Returns(existing);
        var useCase = new DeleteRunbookDocument(store, new FixedTimeProvider(CurrentTime));

        var result = await useCase.ExecuteAsync(new DeleteRunbookDocumentRequest("runbook"), CancellationToken.None);

        Assert.Equal(RunbookDocumentOperationStatus.Deleted, result.Status);
        await store.Received(1).DeleteAsync(
            Arg.Is<KnowledgeDocument>(document =>
                document.Id == "runbook"
                && document.Status == KnowledgeDocumentStatus.Deleted
                && document.Content == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsyncPropagatesDeleteFailureAndCancellationToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var store = Substitute.For<IRunbookDocumentStore>();
        var existing = KnowledgeDocument.Create(
            "runbook",
            "runbook.md",
            "Content",
            CurrentTime.AddDays(-1));
        store.FindByIdAsync("runbook", cancellationToken)
            .Returns(existing);
        store.DeleteAsync(Arg.Any<KnowledgeDocument>(), cancellationToken)
            .Returns<Task>(_ => throw new InvalidOperationException("Storage unavailable."));
        var useCase = new DeleteRunbookDocument(store, new FixedTimeProvider(CurrentTime));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(
                new DeleteRunbookDocumentRequest("runbook"),
                cancellationToken));

        await store.Received(1)
            .DeleteAsync(Arg.Any<KnowledgeDocument>(), cancellationToken);
    }

    [Fact]
    public async Task DeleteAsyncForMissingDocumentReturnsNotFound()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        store.FindByIdAsync("missing", Arg.Any<CancellationToken>())
            .Returns((KnowledgeDocument?)null);
        var useCase = new DeleteRunbookDocument(store, new FixedTimeProvider(CurrentTime));

        var result = await useCase.ExecuteAsync(
            new DeleteRunbookDocumentRequest("missing"),
            CancellationToken.None);

        Assert.Equal(RunbookDocumentOperationStatus.NotFound, result.Status);
        await store.DidNotReceive()
            .DeleteAsync(Arg.Any<KnowledgeDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsyncForDeletedDocumentReturnsNotFound()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        var deletedDocument = KnowledgeDocument.Create(
            "runbook",
            "runbook.md",
            "Content",
            CurrentTime.AddDays(-1)).Delete(CurrentTime);
        store.FindByIdAsync("runbook", Arg.Any<CancellationToken>())
            .Returns(deletedDocument);
        var useCase = new DeleteRunbookDocument(store, new FixedTimeProvider(CurrentTime));

        var result = await useCase.ExecuteAsync(
            new DeleteRunbookDocumentRequest("runbook"),
            CancellationToken.None);

        Assert.Equal(RunbookDocumentOperationStatus.NotFound, result.Status);
        await store.DidNotReceive()
            .DeleteAsync(Arg.Any<KnowledgeDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsyncForInvalidIdReturnsValidationErrorWithoutStoreAccess()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        var useCase = new DeleteRunbookDocument(store, new FixedTimeProvider(CurrentTime));

        var result = await useCase.ExecuteAsync(
            new DeleteRunbookDocumentRequest("../runbook"),
            CancellationToken.None);

        Assert.Equal(RunbookDocumentOperationStatus.ValidationError, result.Status);
        Assert.Equal(RunbookDocumentValidationErrorCode.InvalidDocumentId, result.ValidationError);
        await store.DidNotReceive()
            .FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsyncReturnsDocumentMetadataWithoutContent()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        var activeDocument = KnowledgeDocument.Create("active", "active.md", "Active content", CurrentTime);
        var deletedDocument = activeDocument.Delete(CurrentTime.AddMinutes(1));
        store.ListAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<RunbookDocumentSummary>>(
                [
                    RunbookDocumentSummary.From(activeDocument, chunkCount: 0),
                    RunbookDocumentSummary.From(deletedDocument, chunkCount: 0)
                ]);
        var useCase = new ListRunbookDocuments(store);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Collection(
            result.Documents,
            active => Assert.Equal(KnowledgeDocumentStatus.Active, active.Status),
            deleted => Assert.Equal(KnowledgeDocumentStatus.Deleted, deleted.Status));
        Assert.Equal("active", result.Documents[0].DocumentId);
        Assert.Equal(activeDocument.ContentHash, result.Documents[0].ContentHash);
        Assert.DoesNotContain(
            typeof(RunbookDocumentSummary).GetProperties(),
            property => property.Name.Equals("Content", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ListAsyncPropagatesStoreFailure()
    {
        var store = Substitute.For<IRunbookDocumentStore>();
        store.ListAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<RunbookDocumentSummary>>(
                _ => throw new InvalidOperationException("Storage unavailable."));
        var useCase = new ListRunbookDocuments(store);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(CancellationToken.None));
    }

    private static ReplaceRunbookDocument CreateReplaceUseCase(
        IRunbookDocumentStore store,
        ITextEmbeddingGenerator? embeddingGenerator = null)
    {
        var options = new RunbookDocumentOptions { MaxDocumentSizeBytes = 1_048_576 };
        var knowledgeIndex = Substitute.For<IRunbookKnowledgeIndex>();
        knowledgeIndex.IndexAsync(
                Arg.Any<KnowledgeDocument>(),
                Arg.Any<IReadOnlyList<DocumentChunk>>(),
                Arg.Any<IReadOnlyList<RunbookChunkEmbedding>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => store.SaveAsync(
                call.Arg<KnowledgeDocument>(),
                call.Arg<IReadOnlyList<DocumentChunk>>(),
                call.Arg<IReadOnlyList<RunbookChunkEmbedding>>(),
                call.Arg<CancellationToken>()));

        if (embeddingGenerator is null)
        {
            embeddingGenerator = Substitute.For<ITextEmbeddingGenerator>();
            embeddingGenerator.ModelId.Returns("test-embedding-model");
            embeddingGenerator.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ReadOnlyMemory<float>>(TestEmbedding));
        }

        return new(
            store,
            knowledgeIndex,
            new RunbookDocumentValidator(Options.Create(options)),
            new FixedTimeProvider(CurrentTime),
            new RunbookDocumentChunker(Options.Create(new RunbookChunkingOptions())),
            embeddingGenerator);
    }

    private static ReplaceRunbookDocumentRequest CreateReplaceRequest(string id, string fileName, string content)
    {
        return new(id, fileName, Encoding.UTF8.GetBytes(content));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
