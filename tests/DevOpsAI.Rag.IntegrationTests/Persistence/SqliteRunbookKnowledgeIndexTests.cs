using System.Text.Json;
using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Documents;
using DevOpsAI.Rag.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.IntegrationTests.Persistence;

public sealed class SqliteRunbookKnowledgeIndexTests
{
    private static readonly DateTimeOffset TestTime = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly float[] QueryX = [1f, 0f];
    private static readonly float[] QueryY = [0f, 1f];
    private static readonly float[] DiagonalVector = [0.8f, 0.6f];
    private static readonly float[] EvaluationCacheVector = [1f, 0f];
    private static readonly float[] EvaluationDeploymentVector = [0f, 1f];
    private static readonly float[] EvaluationAlertVector = [0.6f, 0.8f];
    private static readonly string[] RankedChunkIds = ["cache-recovery", "deployment-rollback"];
    private static readonly string[] LimitedChunkIds = ["chunk-c", "chunk-a"];
    private static readonly JsonSerializerOptions EvaluationJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SearchAsyncRanksCandidatesAndMapsTheirSourceMetadata()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var firstDocument = CreateDocument("cache", "cache.md", "## Recovery\nRestart the cache.");
        var firstChunk = CreateChunk(firstDocument, "cache-recovery", "Restart the cache.", "Recovery", 2);
        var secondDocument = CreateDocument(
            "deployment",
            "deployment.md",
            "## Rollback\nRestore the previous release.");
        var secondChunk = CreateChunk(
            secondDocument,
            "deployment-rollback",
            "Restore the previous release.",
            "Rollback",
            2);

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            await store.SaveAsync(
                firstDocument,
                new List<DocumentChunk> { firstChunk },
                new List<RunbookChunkEmbedding> { new(firstChunk.Id, "test-model", QueryX) },
                CancellationToken.None);
            await store.SaveAsync(
                secondDocument,
                new List<DocumentChunk> { secondChunk },
                new List<RunbookChunkEmbedding> { new(secondChunk.Id, "test-model", DiagonalVector) },
                CancellationToken.None);
        }

        await using (var context = database.CreateContext())
        {
            var index = CreateIndex(context, threshold: 0.5, topK: 5);

            var result = await index.SearchAsync("test-model", QueryX, CancellationToken.None);

            Assert.Equal(RunbookEvidenceSufficiency.Sufficient, result.Sufficiency);
            Assert.Equal(
                RankedChunkIds,
                result.Candidates.Select(candidate => candidate.Chunk.Id));
            Assert.Equal(1d, result.Candidates[0].RelevanceScore, 6);
            Assert.Equal("cache", result.Candidates[0].Chunk.Document.Id);
            Assert.Equal("cache.md", result.Candidates[0].Chunk.Document.FileName);
            Assert.Equal("Recovery", result.Candidates[0].Chunk.Heading);
            Assert.Equal(2, result.Candidates[0].Chunk.StartLine);
            Assert.Equal(2, result.Candidates[0].Chunk.EndLine);
        }
    }

    [Fact]
    public async Task SearchAsyncReturnsInsufficientWhenNoCandidateMeetsThreshold()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var document = CreateDocument("cache", "cache.md", "## Recovery\nRestart cache.");
        var chunk = CreateChunk(document, "cache-chunk", "Restart cache.", "Recovery", 2);
        await SaveSnapshotAsync(
            database,
            document,
            new List<DocumentChunk> { chunk },
            new List<RunbookChunkEmbedding> { new(chunk.Id, "test-model", QueryX) });

        await using var context = database.CreateContext();
        var index = CreateIndex(context, threshold: 0.8, topK: 5);

        var result = await index.SearchAsync("test-model", QueryY, CancellationToken.None);

        Assert.Equal(RunbookEvidenceSufficiency.Insufficient, result.Sufficiency);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task SearchAsyncAppliesConfiguredTopKAndOrdersTiesDeterministically()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var document = CreateDocument("cache", "cache.md", "One\nTwo\nThree");
        var chunks = new[]
        {
            new DocumentChunk("chunk-c", document, 0, "One", null, 1, 1),
            new DocumentChunk("chunk-a", document, 1, "Two", null, 2, 2),
            new DocumentChunk("chunk-b", document, 2, "Three", null, 3, 3)
        };
        var embeddings = chunks.Select(chunk =>
            new RunbookChunkEmbedding(chunk.Id, "test-model", QueryX)).ToArray();
        await SaveSnapshotAsync(database, document, chunks, embeddings);

        await using var context = database.CreateContext();
        var index = CreateIndex(context, threshold: 0, topK: 2);

        var result = await index.SearchAsync("test-model", QueryX, CancellationToken.None);

        Assert.Equal(LimitedChunkIds, result.Candidates.Select(candidate => candidate.Chunk.Id));
    }

    [Fact]
    public async Task SearchAsyncRejectsVectorsFromADifferentEmbeddingModel()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var document = CreateDocument("cache", "cache.md", "## Recovery\nRestart cache.");
        var chunk = CreateChunk(document, "cache-chunk", "Restart cache.", "Recovery", 2);
        await SaveSnapshotAsync(
            database,
            document,
            new List<DocumentChunk> { chunk },
            new List<RunbookChunkEmbedding> { new(chunk.Id, "old-model", QueryX) });

        await using var context = database.CreateContext();
        var index = CreateIndex(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => index.SearchAsync("new-model", QueryX, CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsyncPropagatesStorageFailuresInsteadOfReturningInsufficientEvidence()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var context = database.CreateContext();
        await context.DisposeAsync();
        var index = CreateIndex(context);

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => index.SearchAsync("test-model", QueryX, CancellationToken.None));
    }

    [Fact]
    public async Task ReplacingSnapshotReplacesItsStoredEmbedding()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var original = CreateDocument("cache", "cache.md", "## Recovery\nRestart cache.");
        var originalChunk = CreateChunk(original, "old-chunk", "Restart cache.", "Recovery", 2);
        await SaveSnapshotAsync(
            database,
            original,
            new List<DocumentChunk> { originalChunk },
            new List<RunbookChunkEmbedding> { new(originalChunk.Id, "test-model", QueryX) });

        var replacement = original.Replace(
            "cache-recovery-v2.md",
            "## Recovery\nClear the cache.",
            TestTime.AddMinutes(1));
        var replacementChunk = CreateChunk(
            replacement,
            "new-chunk",
            "Clear the cache.",
            "Recovery",
            2);
        await SaveSnapshotAsync(
            database,
            replacement,
            new List<DocumentChunk> { replacementChunk },
            new List<RunbookChunkEmbedding> { new(replacementChunk.Id, "test-model", QueryY) });

        await using var verificationContext = database.CreateContext();
        var storedEmbeddings = await verificationContext.ChunkEmbeddings
            .AsNoTracking()
            .ToListAsync();

        var storedEmbedding = Assert.Single(storedEmbeddings);
        Assert.Equal("new-chunk", storedEmbedding.ChunkId);
        Assert.Equal("test-model", storedEmbedding.ModelId);
        Assert.Equal(2, storedEmbedding.Dimensions);
    }

    [Fact]
    public async Task FailedReplacementRollsBackChunkAndEmbeddingChangesTogether()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var original = CreateDocument("cache", "cache.md", "## Recovery\nRestart cache.");
        var originalChunk = CreateChunk(original, "old-chunk", "Restart cache.", "Recovery", 2);
        await SaveSnapshotAsync(
            database,
            original,
            new List<DocumentChunk> { originalChunk },
            new List<RunbookChunkEmbedding> { new(originalChunk.Id, "test-model", QueryX) });
        byte[] originalVectorBlob;
        await using (var context = database.CreateContext())
        {
            originalVectorBlob = await context.ChunkEmbeddings
                .Select(embedding => embedding.VectorBlob)
                .SingleAsync();
        }

        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER RejectEmbeddingReplacement
                BEFORE UPDATE ON Documents
                BEGIN
                    SELECT RAISE(ABORT, 'forced persistence failure');
                END;
                """);
        }

        var replacement = original.Replace(
            "cache-recovery-v2.md",
            "## Recovery\nClear the cache.",
            TestTime.AddMinutes(1));
        var replacementChunk = CreateChunk(replacement, "new-chunk", "Clear the cache.", "Recovery", 2);

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            await Assert.ThrowsAsync<DbUpdateException>(
                () => store.SaveAsync(
                    replacement,
                    new List<DocumentChunk> { replacementChunk },
                    new List<RunbookChunkEmbedding>
                    {
                        new(replacementChunk.Id, "test-model", QueryY)
                    },
                    CancellationToken.None));
        }

        await using var verificationContext = database.CreateContext();
        var persistedDocument = await new SqliteRunbookDocumentStore(verificationContext)
            .FindByIdAsync(original.Id, CancellationToken.None);
        var persistedChunk = await verificationContext.Chunks.SingleAsync();
        var persistedEmbedding = await verificationContext.ChunkEmbeddings.SingleAsync();

        Assert.NotNull(persistedDocument);
        Assert.Equal(original.Content, persistedDocument.Content);
        Assert.Equal("old-chunk", persistedChunk.Id);
        Assert.Equal("old-chunk", persistedEmbedding.ChunkId);
        Assert.Equal(originalVectorBlob, persistedEmbedding.VectorBlob);
    }

    [Fact]
    public async Task RetrievalEvaluationFixtureFindsKnownRelevantChunksOffline()
    {
        await using var database = await TemporaryDatabase.CreateAsync();
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "rag-evaluation.json");
        await using var fixtureStream = File.OpenRead(fixturePath);
        var evaluationCases = await JsonSerializer.DeserializeAsync<RetrievalEvaluationCase[]>(
            fixtureStream,
            EvaluationJsonOptions)
            ?? throw new InvalidDataException("The retrieval evaluation fixture is empty or invalid.");

        var corpus = new[]
        {
            CreateEvaluationDocument(
                "cache-runbook",
                "cache.md",
                "cache-recovery",
                "## Recovery\nRestart the cache.",
                "Recovery",
                EvaluationCacheVector),
            CreateEvaluationDocument(
                "deployment-runbook",
                "deployment.md",
                "deployment-rollback",
                "## Rollback\nRestore the previous release.",
                "Rollback",
                EvaluationDeploymentVector),
            CreateEvaluationDocument(
                "alert-runbook",
                "alerts.md",
                "alert-investigation",
                "## Investigation\nInspect the alert source.",
                "Investigation",
                EvaluationAlertVector)
        };

        await using (var context = database.CreateContext())
        {
            var store = new SqliteRunbookDocumentStore(context);
            foreach (var item in corpus)
            {
                await store.SaveAsync(
                    item.Document,
                    new List<DocumentChunk> { item.Chunk },
                    new List<RunbookChunkEmbedding>
                    {
                        new(item.Chunk.Id, "synthetic-evaluation-model", item.Embedding)
                    },
                    CancellationToken.None);
            }
        }

        double reciprocalRankTotal = 0;
        var relevantHitCount = 0;
        await using (var context = database.CreateContext())
        {
            var index = CreateIndex(context, threshold: 0.9, topK: 3);
            foreach (var evaluationCase in evaluationCases)
            {
                var result = await index.SearchAsync(
                    "synthetic-evaluation-model",
                    evaluationCase.QueryEmbedding,
                    CancellationToken.None);
                var rank = result.Candidates
                    .Select((candidate, candidateIndex) => new { candidate.Chunk.Id, candidateIndex })
                    .Where(item => evaluationCase.RelevantChunkIds.Contains(item.Id, StringComparer.Ordinal))
                    .Select(item => item.candidateIndex + 1)
                    .DefaultIfEmpty(0)
                    .Min();

                Assert.True(
                    rank > 0,
                    $"No relevant chunk was retrieved for the evaluation question '{evaluationCase.Question}'.");
                relevantHitCount++;
                reciprocalRankTotal += 1d / rank;
            }
        }

        var recallAtK = relevantHitCount / (double)evaluationCases.Length;
        var meanReciprocalRank = reciprocalRankTotal / evaluationCases.Length;

        Assert.Equal(1d, recallAtK, 6);
        Assert.Equal(1d, meanReciprocalRank, 6);
    }

    private static SqliteRunbookKnowledgeIndex CreateIndex(
        RagDbContext context,
        double threshold = 0.7,
        int topK = 5)
    {
        return new SqliteRunbookKnowledgeIndex(
            context,
            new SqliteRunbookDocumentStore(context),
            Options.Create(new RunbookRetrievalOptions
            {
                MinimumRelevanceScore = threshold,
                TopK = topK
            }));
    }

    private static async Task SaveSnapshotAsync(
        TemporaryDatabase database,
        KnowledgeDocument document,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<RunbookChunkEmbedding> embeddings)
    {
        await using var context = database.CreateContext();
        await new SqliteRunbookDocumentStore(context)
            .SaveAsync(document, chunks, embeddings, CancellationToken.None);
    }

    private static KnowledgeDocument CreateDocument(string id, string fileName, string content)
    {
        return KnowledgeDocument.Create(id, fileName, content, TestTime);
    }

    private static DocumentChunk CreateChunk(
        KnowledgeDocument document,
        string id,
        string text,
        string? heading,
        int sourceLine)
    {
        return new DocumentChunk(
            id,
            document,
            0,
            text,
            heading,
            sourceLine,
            sourceLine);
    }

    private static EvaluationDocument CreateEvaluationDocument(
        string documentId,
        string fileName,
        string chunkId,
        string content,
        string heading,
        float[] embedding)
    {
        var document = CreateDocument(documentId, fileName, content);
        var chunk = CreateChunk(document, chunkId, content.Split('\n')[1], heading, 2);
        return new EvaluationDocument(document, chunk, embedding);
    }

    private sealed record EvaluationDocument(
        KnowledgeDocument Document,
        DocumentChunk Chunk,
        ReadOnlyMemory<float> Embedding);

    private sealed record RetrievalEvaluationCase(
        string Question,
        float[] QueryEmbedding,
        string[] RelevantChunkIds);

    private sealed class TemporaryDatabase : IAsyncDisposable
    {
        private readonly string _databasePath;

        private TemporaryDatabase(string databasePath)
        {
            _databasePath = databasePath;
        }

        public static async Task<TemporaryDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"rag-index-{Guid.NewGuid():N}.db");
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
