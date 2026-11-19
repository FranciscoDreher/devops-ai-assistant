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
    }

    [Fact]
    public async Task ReplaceAsyncPropagatesSaveFailureAndCancellationToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var store = Substitute.For<IRunbookDocumentStore>();
        store.FindByIdAsync("runbook", cancellationToken)
            .Returns((KnowledgeDocument?)null);
        store.SaveAsync(Arg.Any<KnowledgeDocument>(), cancellationToken)
            .Returns<Task>(_ => throw new InvalidOperationException("Storage unavailable."));
        var useCase = CreateReplaceUseCase(store);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(
                CreateReplaceRequest("runbook", "runbook.md", "content"),
                cancellationToken));

        await store.Received(1)
            .SaveAsync(Arg.Any<KnowledgeDocument>(), cancellationToken);
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
                [RunbookDocumentSummary.From(activeDocument), RunbookDocumentSummary.From(deletedDocument)]);
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

    private static ReplaceRunbookDocument CreateReplaceUseCase(IRunbookDocumentStore store)
    {
        var options = new RunbookDocumentOptions { MaxDocumentSizeBytes = 1_048_576 };
        return new(
            store,
            new RunbookDocumentValidator(Options.Create(options)),
            new FixedTimeProvider(CurrentTime));
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
