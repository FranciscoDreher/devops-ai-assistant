using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Chat;
using DevOpsAI.Rag.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.Infrastructure.Persistence;

public sealed class SqliteRunbookKnowledgeIndex : IRunbookKnowledgeIndex
{
    private readonly RagDbContext _dbContext;
    private readonly IRunbookDocumentStore _documentStore;
    private readonly RunbookRetrievalOptions _options;

    public SqliteRunbookKnowledgeIndex(
        RagDbContext dbContext,
        IRunbookDocumentStore documentStore,
        IOptions<RunbookRetrievalOptions> options)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(documentStore);
        ArgumentNullException.ThrowIfNull(options);
        options.Value.Validate();

        _dbContext = dbContext;
        _documentStore = documentStore;
        _options = options.Value;
    }

    public Task IndexAsync(
        KnowledgeDocument document,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<RunbookChunkEmbedding> embeddings,
        CancellationToken cancellationToken)
    {
        return _documentStore.SaveAsync(document, chunks, embeddings, cancellationToken);
    }

    public async Task<RunbookRetrievalResult> SearchAsync(
        string embeddingModelId,
        ReadOnlyMemory<float> queryEmbedding,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(embeddingModelId);
        ValidateVector(queryEmbedding, nameof(queryEmbedding));

        var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            var activeChunkCount = await _dbContext.Chunks
                .CountAsync(chunk => chunk.Document.Status == KnowledgeDocumentStatus.Active, cancellationToken)
                .ConfigureAwait(false);

            if (activeChunkCount == 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return InsufficientEvidence();
            }

            var storedEmbeddings = await _dbContext.ChunkEmbeddings
                .AsNoTracking()
                .Where(embedding => embedding.Chunk.Document.Status == KnowledgeDocumentStatus.Active)
                .Select(embedding => new StoredChunkEmbedding(
                    embedding.ChunkId,
                    embedding.ModelId,
                    embedding.Dimensions,
                    embedding.VectorBlob,
                    embedding.Chunk.DocumentId,
                    embedding.Chunk.DocumentVersion,
                    embedding.Chunk.Ordinal,
                    embedding.Chunk.Text,
                    embedding.Chunk.Heading,
                    embedding.Chunk.StartLine,
                    embedding.Chunk.EndLine))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (storedEmbeddings.Count != activeChunkCount)
            {
                throw new InvalidOperationException(
                    "The local index is incomplete; reindex active documents before searching.");
            }

            if (storedEmbeddings.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return InsufficientEvidence();
            }

            EnsureCompatibleEmbeddingSpace(storedEmbeddings, embeddingModelId, queryEmbedding.Length);

            var rankedEmbeddings = storedEmbeddings
                .Select(embedding => new ScoredChunk(
                    embedding,
                    CalculateCosineSimilarity(
                        queryEmbedding.Span,
                        SqliteEmbeddingVectorConverter.Deserialize(embedding.VectorBlob, embedding.Dimensions))))
                .Where(candidate => candidate.Score >= _options.MinimumRelevanceScore)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Embedding.DocumentId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Embedding.Ordinal)
                .ThenBy(candidate => candidate.Embedding.ChunkId, StringComparer.Ordinal)
                .Take(_options.TopK)
                .ToArray();

            if (rankedEmbeddings.Length == 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return InsufficientEvidence();
            }

            var documentIds = rankedEmbeddings
                .Select(candidate => candidate.Embedding.DocumentId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var documentEntities = await _dbContext.Documents
                .AsNoTracking()
                .Where(document => documentIds.Contains(document.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var documents = documentEntities.ToDictionary(document => document.Id, StringComparer.Ordinal);
            var candidates = new List<RetrievalCandidate>(rankedEmbeddings.Length);

            foreach (var scoredChunk in rankedEmbeddings)
            {
                if (!documents.TryGetValue(scoredChunk.Embedding.DocumentId, out var documentEntity)
                    || documentEntity.Status != KnowledgeDocumentStatus.Active
                    || documentEntity.Version != scoredChunk.Embedding.DocumentVersion
                    || documentEntity.Content is null)
                {
                    throw new InvalidDataException("A retrieved chunk does not match its active source document.");
                }

                var document = KnowledgeDocument.Reconstitute(
                    documentEntity.Id,
                    documentEntity.FileName,
                    documentEntity.Content,
                    documentEntity.Version,
                    documentEntity.ContentHash,
                    documentEntity.Status,
                    documentEntity.CreatedAtUtc,
                    documentEntity.UpdatedAtUtc,
                    documentEntity.DeletedAtUtc);
                var chunk = new DocumentChunk(
                    scoredChunk.Embedding.ChunkId,
                    document,
                    scoredChunk.Embedding.Ordinal,
                    scoredChunk.Embedding.Text,
                    scoredChunk.Embedding.Heading,
                    scoredChunk.Embedding.StartLine,
                    scoredChunk.Embedding.EndLine);
                candidates.Add(new RetrievalCandidate(chunk, scoredChunk.Score));
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new RunbookRetrievalResult(candidates, RunbookEvidenceSufficiency.Sufficient);
        }
    }

    private static RunbookRetrievalResult InsufficientEvidence()
    {
        return new RunbookRetrievalResult([], RunbookEvidenceSufficiency.Insufficient);
    }

    private static void EnsureCompatibleEmbeddingSpace(
        IReadOnlyList<StoredChunkEmbedding> embeddings,
        string modelId,
        int dimensions)
    {
        foreach (var embedding in embeddings)
        {
            if (!string.Equals(embedding.ModelId, modelId, StringComparison.Ordinal)
                || embedding.Dimensions != dimensions)
            {
                throw new InvalidOperationException(
                    "The query embedding model or dimensions do not match the stored index; reindex is required.");
            }
        }
    }

    private static void ValidateVector(ReadOnlyMemory<float> vector, string parameterName)
    {
        if (vector.IsEmpty)
        {
            throw new ArgumentException("The embedding vector cannot be empty.", parameterName);
        }

        var hasNonZeroValue = false;
        foreach (var value in vector.Span)
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentException("The embedding vector must contain finite values.", parameterName);
            }

            hasNonZeroValue |= value != 0;
        }

        if (!hasNonZeroValue)
        {
            throw new ArgumentException("The embedding vector cannot be zero.", parameterName);
        }
    }

    private static double CalculateCosineSimilarity(ReadOnlySpan<float> first, ReadOnlySpan<float> second)
    {
        if (first.Length != second.Length)
        {
            throw new InvalidDataException("The query and stored embedding dimensions do not match.");
        }

        double dotProduct = 0;
        double firstMagnitude = 0;
        double secondMagnitude = 0;

        for (var index = 0; index < first.Length; index++)
        {
            var firstValue = (double)first[index];
            var secondValue = (double)second[index];
            dotProduct += firstValue * secondValue;
            firstMagnitude += firstValue * firstValue;
            secondMagnitude += secondValue * secondValue;
        }

        if (firstMagnitude == 0 || secondMagnitude == 0)
        {
            throw new InvalidDataException("Cosine similarity requires non-zero vectors.");
        }

        var cosineSimilarity = dotProduct / Math.Sqrt(firstMagnitude * secondMagnitude);
        return Math.Clamp(cosineSimilarity, 0, 1);
    }

    private sealed record StoredChunkEmbedding(
        string ChunkId,
        string ModelId,
        int Dimensions,
        byte[] VectorBlob,
        string DocumentId,
        int DocumentVersion,
        int Ordinal,
        string Text,
        string? Heading,
        int StartLine,
        int EndLine);

    private sealed record ScoredChunk(StoredChunkEmbedding Embedding, double Score);
}
