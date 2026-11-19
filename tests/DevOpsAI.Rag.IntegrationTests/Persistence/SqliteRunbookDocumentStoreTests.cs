using DevOpsAI.Rag.Domain.Documents;
using DevOpsAI.Rag.Infrastructure.Persistence;
using DevOpsAI.Rag.Infrastructure.Persistence.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DevOpsAI.Rag.IntegrationTests.Persistence;

public sealed class SqliteRunbookDocumentStoreTests
{
    [Fact]
    public async Task SaveAsyncPersistsDocumentChunksAndReferencesAcrossReopen()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var document = CreateDocument();
        var chunk = CreateChunk(document, "chunk-1", "Restart the cache", 1, 2);

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            await store.SaveAsync(document, [chunk], CancellationToken.None);
        }

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            var restoredDocument = await store.FindByIdAsync("cache-runbook", CancellationToken.None);
            var restoredChunks = await context.Chunks
                .Include(entity => entity.CitationReference)
                .ToListAsync();

            Assert.NotNull(restoredDocument);
            Assert.Equal(document.Id, restoredDocument.Id);
            Assert.Equal(document.Content, restoredDocument.Content);
            Assert.Equal(document.ContentHash, restoredDocument.ContentHash);
            Assert.Equal(document.Version, restoredDocument.Version);
            Assert.Equal(document.CreatedAtUtc, restoredDocument.CreatedAtUtc);
            Assert.Equal(document.UpdatedAtUtc, restoredDocument.UpdatedAtUtc);
            var persistedChunk = Assert.Single(restoredChunks);
            Assert.Equal(chunk.Text, persistedChunk.Text);
            Assert.NotNull(persistedChunk.CitationReference);
            Assert.Equal(chunk.Id, persistedChunk.CitationReference.ChunkId);
            Assert.Equal(document.Id, persistedChunk.CitationReference.DocumentId);
            Assert.Equal(document.Version, persistedChunk.CitationReference.DocumentVersion);
            Assert.Equal(chunk.StartLine, persistedChunk.CitationReference.StartLine);
            Assert.Equal(chunk.EndLine, persistedChunk.CitationReference.EndLine);
        }
    }

    [Fact]
    public async Task SaveAsyncReplacesDocumentAndRemovesPreviousChunksAndReferences()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var original = CreateDocument();
        var originalChunk = CreateChunk(original, "old-chunk", "Old recovery", 1, 2);

        await using (var context = database.CreateContext())
        {
            await new SqliteRunbookDocumentStore(context)
                .SaveAsync(original, [originalChunk], CancellationToken.None);
        }

        var replacement = original.Replace(
            "cache-recovery-v2.md",
            "## Recovery\nRestart the cache safely.",
            TestTime.AddMinutes(1));
        var replacementChunk = CreateChunk(replacement, "new-chunk", "Restart the cache safely.", 2, 2);

        await using (var context = database.CreateContext())
        {
            await new SqliteRunbookDocumentStore(context)
                .SaveAsync(replacement, [replacementChunk], CancellationToken.None);
        }

        await using (var context = database.CreateContext())
        {
            var persistedDocument = await new SqliteRunbookDocumentStore(context)
                .FindByIdAsync(replacement.Id, CancellationToken.None);
            var persistedChunks = await context.Chunks
                .Include(entity => entity.CitationReference)
                .ToListAsync();

            Assert.NotNull(persistedDocument);
            Assert.Equal(2, persistedDocument.Version);
            Assert.Equal(replacement.Content, persistedDocument.Content);
            var persistedChunk = Assert.Single(persistedChunks);
            Assert.Equal("new-chunk", persistedChunk.Id);
            Assert.Equal("new-chunk", persistedChunk.CitationReference?.ChunkId);
        }
    }

    [Fact]
    public async Task SaveAsyncRollsBackReplacementWhenDatabaseRejectsTheWrite()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var original = CreateDocument();
        var originalChunk = CreateChunk(original, "old-chunk", "Old recovery", 1, 2);

        await using (var context = database.CreateContext())
        {
            await new SqliteRunbookDocumentStore(context)
                .SaveAsync(original, [originalChunk], CancellationToken.None);
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER RejectDocumentUpdate
                BEFORE UPDATE ON Documents
                BEGIN
                    SELECT RAISE(ABORT, 'forced persistence failure');
                END;
                """);
        }

        var replacement = original.Replace(
            "cache-recovery-v2.md",
            "## Recovery\nNew content.",
            TestTime.AddMinutes(1));
        var replacementChunk = CreateChunk(replacement, "new-chunk", "New content.", 2, 2);

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => store.SaveAsync(replacement, [replacementChunk], CancellationToken.None));
            Assert.IsType<SqliteException>(exception.InnerException);
        }

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            var persistedDocument = await store.FindByIdAsync(original.Id, CancellationToken.None);
            var persistedChunks = await context.Chunks
                .Include(entity => entity.CitationReference)
                .ToListAsync();

            Assert.NotNull(persistedDocument);
            Assert.Equal(original.Content, persistedDocument.Content);
            Assert.Equal(original.Version, persistedDocument.Version);
            var persistedChunk = Assert.Single(persistedChunks);
            Assert.Equal("old-chunk", persistedChunk.Id);
            Assert.Equal("old-chunk", persistedChunk.CitationReference?.ChunkId);
        }
    }

    [Fact]
    public async Task SaveAsyncRejectsAStaleReplacementWithoutChangingTheCurrentSnapshot()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var original = CreateDocument();
        var originalChunk = CreateChunk(original, "original-chunk", "Original content", 1, 2);

        await using (var context = database.CreateContext())
        {
            await new SqliteRunbookDocumentStore(context)
                .SaveAsync(original, [originalChunk], CancellationToken.None);
        }

        var currentReplacement = original.Replace(
            "cache-recovery-current.md",
            "## Recovery\nCurrent content.",
            TestTime.AddMinutes(1));
        var currentChunk = CreateChunk(currentReplacement, "current-chunk", "Current content.", 2, 2);

        await using (var context = database.CreateContext())
        {
            await new SqliteRunbookDocumentStore(context)
                .SaveAsync(currentReplacement, [currentChunk], CancellationToken.None);
        }

        var staleReplacement = original.Replace(
            "cache-recovery-stale.md",
            "## Recovery\nStale content.",
            TestTime.AddMinutes(2));
        var staleChunk = CreateChunk(staleReplacement, "stale-chunk", "Stale content.", 2, 2);

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => store.SaveAsync(staleReplacement, [staleChunk], CancellationToken.None));
        }

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            var persistedDocument = await store.FindByIdAsync(original.Id, CancellationToken.None);
            var persistedChunk = Assert.Single(await context.Chunks.ToListAsync());

            Assert.NotNull(persistedDocument);
            Assert.Equal(currentReplacement.Content, persistedDocument.Content);
            Assert.Equal("current-chunk", persistedChunk.Id);
        }
    }

    [Fact]
    public async Task DeleteAsyncPersistsTombstoneAndRemovesChunksAndReferences()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var document = CreateDocument();
        var chunk = CreateChunk(document, "chunk-1", "Restart the cache", 1, 2);

        await using (var context = database.CreateContext())
        {
            await new SqliteRunbookDocumentStore(context)
                .SaveAsync(document, [chunk], CancellationToken.None);
        }

        var tombstone = document.Delete(TestTime.AddMinutes(1));
        await using (var context = database.CreateContext())
        {
            await new SqliteRunbookDocumentStore(context).DeleteAsync(tombstone, CancellationToken.None);
        }

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            var persistedTombstone = await store.FindByIdAsync(document.Id, CancellationToken.None);
            var summary = Assert.Single(await store.ListAsync(CancellationToken.None));

            Assert.NotNull(persistedTombstone);
            Assert.Equal(KnowledgeDocumentStatus.Deleted, persistedTombstone.Status);
            Assert.Null(persistedTombstone.Content);
            Assert.Equal(document.ContentHash, persistedTombstone.ContentHash);
            Assert.Equal(KnowledgeDocumentStatus.Deleted, summary.Status);
            Assert.Empty(await context.Chunks.ToListAsync());
            Assert.Empty(await context.CitationReferences.ToListAsync());
        }
    }

    [Fact]
    public async Task SaveAsyncRejectsChunksFromAnotherDocument()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var document = CreateDocument();
        var unrelatedDocument = KnowledgeDocument.Create(
            "other-runbook",
            "other.md",
            "## Recovery\nOther content.",
            TestTime);
        var unrelatedChunk = CreateChunk(unrelatedDocument, "other-chunk", "Other content.", 2, 2);

        await using var context = database.CreateContext();
        var store = new SqliteRunbookDocumentStore(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => store.SaveAsync(document, [unrelatedChunk], CancellationToken.None));

        Assert.Empty(await context.Documents.ToListAsync());
    }

    private static KnowledgeDocument CreateDocument()
    {
        return KnowledgeDocument.Create(
            "cache-runbook",
            "cache-recovery.md",
            "## Recovery\nRestart the cache.",
            TestTime);
    }

    private static DocumentChunk CreateChunk(
        KnowledgeDocument document,
        string id,
        string text,
        int startLine,
        int endLine)
    {
        return new DocumentChunk(id, document, 0, text, "Recovery", startLine, endLine);
    }

    private static readonly DateTimeOffset TestTime = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private sealed class TemporaryDatabase : IAsyncDisposable
    {
        private readonly string _databasePath;

        private TemporaryDatabase(string databasePath)
        {
            _databasePath = databasePath;
        }

        public static async Task<TemporaryDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"rag-persistence-{Guid.NewGuid():N}.db");
            var database = new TemporaryDatabase(databasePath);

            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync();

            return database;
        }

        public RagDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RagDbContext>()
                .UseSqlite(new SqliteConnectionStringBuilder
                {
                    DataSource = _databasePath,
                    ForeignKeys = true,
                    Pooling = false
                }.ToString())
                .Options;

            return new RagDbContext(options);
        }

        public ValueTask DisposeAsync()
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }

            return ValueTask.CompletedTask;
        }
    }
}
