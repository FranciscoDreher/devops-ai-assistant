using DevOpsAI.Rag.Application.Documents;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Chat;
using DevOpsAI.Rag.Domain.Documents;
using DevOpsAI.Rag.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevOpsAI.Rag.Infrastructure.Persistence;

public sealed class SqliteRunbookDocumentStore(RagDbContext dbContext) : IRunbookDocumentStore
{
    public async Task<KnowledgeDocument?> FindByIdAsync(
        string documentId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(document => document.Id == documentId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToDomain(entity);
    }

    public Task SaveAsync(KnowledgeDocument document, CancellationToken cancellationToken)
    {
        return SaveAsync(document, Array.Empty<DocumentChunk>(), cancellationToken);
    }

    public async Task SaveAsync(
        KnowledgeDocument document,
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        await SaveSnapshotAsync(
            document,
            chunks,
            Array.Empty<RunbookChunkEmbedding>(),
            allowUnembeddedChunks: true,
            cancellationToken).ConfigureAwait(false);
    }

    public Task SaveAsync(
        KnowledgeDocument document,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<RunbookChunkEmbedding> embeddings,
        CancellationToken cancellationToken)
    {
        return SaveSnapshotAsync(
            document,
            chunks,
            embeddings,
            allowUnembeddedChunks: false,
            cancellationToken);
    }

    private async Task SaveSnapshotAsync(
        KnowledgeDocument document,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<RunbookChunkEmbedding> embeddings,
        bool allowUnembeddedChunks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(embeddings);
        EnsureActiveDocument(document);

        foreach (var chunk in chunks)
        {
            EnsureChunkBelongsToDocument(chunk, document);
        }

        EnsureEmbeddingsMatchChunks(chunks, embeddings, allowUnembeddedChunks);

        var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using (transaction.ConfigureAwait(false))
        {
            var existing = await dbContext.Documents
                .SingleOrDefaultAsync(entity => entity.Id == document.Id, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                if (document.Version != 1)
                {
                    throw new InvalidOperationException("A new document must start at version 1.");
                }

                dbContext.Documents.Add(ToEntity(document));
            }
            else
            {
                EnsureNextVersion(document, existing);
                await RemoveChunksAsync(document.Id, cancellationToken).ConfigureAwait(false);
                UpdateEntity(existing, document);
            }

            dbContext.Chunks.AddRange(chunks.Select(ToEntity));
            dbContext.ChunkEmbeddings.AddRange(embeddings.Select(ToEntity));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(KnowledgeDocument tombstone, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tombstone);

        if (tombstone.Status != KnowledgeDocumentStatus.Deleted || tombstone.Content is not null)
        {
            throw new ArgumentException("The document must be a deletion tombstone.", nameof(tombstone));
        }

        var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using (transaction.ConfigureAwait(false))
        {
            var existing = await dbContext.Documents
                .SingleOrDefaultAsync(entity => entity.Id == tombstone.Id, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("The document no longer exists in the store.");

            EnsureTombstoneMatches(tombstone, existing);
            await RemoveChunksAsync(tombstone.Id, cancellationToken).ConfigureAwait(false);
            UpdateEntity(existing, tombstone);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<RunbookDocumentSummary>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .OrderBy(document => document.Id)
            .Select(document => new RunbookDocumentSummary(
                document.Id,
                document.FileName,
                document.ContentHash,
                document.Version,
                document.Status,
                document.Chunks.Count,
                document.CreatedAtUtc,
                document.UpdatedAtUtc,
                document.DeletedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task RemoveChunksAsync(string documentId, CancellationToken cancellationToken)
    {
        await dbContext.Chunks
            .Where(chunk => chunk.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static void EnsureActiveDocument(KnowledgeDocument document)
    {
        if (document.Status != KnowledgeDocumentStatus.Active || document.Content is null)
        {
            throw new ArgumentException("Only active documents can be saved.", nameof(document));
        }
    }

    private static void EnsureChunkBelongsToDocument(DocumentChunk chunk, KnowledgeDocument document)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        if (!string.Equals(chunk.Document.Id, document.Id, StringComparison.Ordinal)
            || chunk.Document.Version != document.Version
            || !string.Equals(chunk.Document.ContentHash, document.ContentHash, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Every chunk must belong to the document snapshot being saved.",
                nameof(chunk));
        }
    }

    private static void EnsureEmbeddingsMatchChunks(
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<RunbookChunkEmbedding> embeddings,
        bool allowUnembeddedChunks)
    {
        if (allowUnembeddedChunks && embeddings.Count == 0)
        {
            return;
        }

        if (embeddings.Count != chunks.Count)
        {
            throw new ArgumentException("Every persisted chunk must have exactly one embedding.", nameof(embeddings));
        }

        var chunkIds = chunks.Select(chunk => chunk.Id).ToHashSet(StringComparer.Ordinal);
        var embeddingChunkIds = new HashSet<string>(StringComparer.Ordinal);
        string? modelId = null;
        var dimensions = 0;

        foreach (var embedding in embeddings)
        {
            ArgumentNullException.ThrowIfNull(embedding);

            if (!chunkIds.Contains(embedding.ChunkId) || !embeddingChunkIds.Add(embedding.ChunkId))
            {
                throw new ArgumentException("Embeddings must map one-to-one to the persisted chunks.", nameof(embeddings));
            }

            modelId ??= embedding.ModelId;
            dimensions = dimensions == 0 ? embedding.Vector.Length : dimensions;
            if (!string.Equals(modelId, embedding.ModelId, StringComparison.Ordinal)
                || dimensions != embedding.Vector.Length)
            {
                throw new ArgumentException("All persisted embeddings must use one model and dimension.", nameof(embeddings));
            }
        }
    }

    private static void EnsureNextVersion(KnowledgeDocument document, RunbookDocumentEntity existing)
    {
        if (document.Version != checked(existing.Version + 1)
            || document.CreatedAtUtc != existing.CreatedAtUtc
            || document.UpdatedAtUtc < existing.UpdatedAtUtc)
        {
            throw new InvalidOperationException("The document changed before the replacement could be saved.");
        }
    }

    private static void EnsureTombstoneMatches(KnowledgeDocument tombstone, RunbookDocumentEntity existing)
    {
        if (existing.Status != KnowledgeDocumentStatus.Active
            || tombstone.Version != existing.Version
            || tombstone.CreatedAtUtc != existing.CreatedAtUtc
            || tombstone.FileName != existing.FileName
            || tombstone.ContentHash != existing.ContentHash
            || tombstone.UpdatedAtUtc < existing.UpdatedAtUtc)
        {
            throw new InvalidOperationException("The document changed before the deletion could be saved.");
        }
    }

    private static RunbookDocumentEntity ToEntity(KnowledgeDocument document)
    {
        return new RunbookDocumentEntity
        {
            Id = document.Id,
            FileName = document.FileName,
            Content = document.Content,
            ContentHash = document.ContentHash,
            Version = document.Version,
            Status = document.Status,
            CreatedAtUtc = document.CreatedAtUtc,
            UpdatedAtUtc = document.UpdatedAtUtc,
            DeletedAtUtc = document.DeletedAtUtc
        };
    }

    private static RunbookChunkEntity ToEntity(DocumentChunk chunk)
    {
        var citation = new CitationReference(chunk);
        return new RunbookChunkEntity
        {
            Id = chunk.Id,
            DocumentId = chunk.Document.Id,
            DocumentVersion = chunk.Document.Version,
            Ordinal = chunk.Ordinal,
            Text = chunk.Text,
            Heading = chunk.Heading,
            StartLine = chunk.StartLine,
            EndLine = chunk.EndLine,
            CitationReference = new RunbookCitationReferenceEntity
            {
                ChunkId = citation.ChunkId,
                DocumentId = citation.DocumentId,
                DocumentVersion = citation.DocumentVersion,
                Heading = citation.Heading,
                StartLine = citation.StartLine,
                EndLine = citation.EndLine
            }
        };
    }

    private static RunbookChunkEmbeddingEntity ToEntity(RunbookChunkEmbedding embedding)
    {
        return new RunbookChunkEmbeddingEntity
        {
            ChunkId = embedding.ChunkId,
            ModelId = embedding.ModelId,
            Dimensions = embedding.Vector.Length,
            VectorBlob = SqliteEmbeddingVectorConverter.Serialize(embedding.Vector)
        };
    }

    private static KnowledgeDocument ToDomain(RunbookDocumentEntity entity)
    {
        return KnowledgeDocument.Reconstitute(
            entity.Id,
            entity.FileName,
            entity.Content,
            entity.Version,
            entity.ContentHash,
            entity.Status,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.DeletedAtUtc);
    }

    private static void UpdateEntity(RunbookDocumentEntity entity, KnowledgeDocument document)
    {
        entity.FileName = document.FileName;
        entity.Content = document.Content;
        entity.ContentHash = document.ContentHash;
        entity.Version = document.Version;
        entity.Status = document.Status;
        entity.CreatedAtUtc = document.CreatedAtUtc;
        entity.UpdatedAtUtc = document.UpdatedAtUtc;
        entity.DeletedAtUtc = document.DeletedAtUtc;
    }
}
