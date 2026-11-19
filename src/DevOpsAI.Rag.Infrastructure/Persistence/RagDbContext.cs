using DevOpsAI.Rag.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevOpsAI.Rag.Infrastructure.Persistence;

public sealed class RagDbContext(DbContextOptions<RagDbContext> options) : DbContext(options)
{
    public DbSet<RunbookDocumentEntity> Documents => Set<RunbookDocumentEntity>();

    public DbSet<RunbookChunkEntity> Chunks => Set<RunbookChunkEntity>();

    public DbSet<RunbookCitationReferenceEntity> CitationReferences => Set<RunbookCitationReferenceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RunbookDocumentEntity>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.Id).ValueGeneratedNever();
            entity.Property(document => document.FileName).IsRequired();
            entity.Property(document => document.Content);
            entity.Property(document => document.ContentHash).IsRequired();
            entity.Property(document => document.Version).IsRequired();
            entity.Property(document => document.Status).HasConversion<string>().IsRequired();
            entity.Property(document => document.CreatedAtUtc).IsRequired();
            entity.Property(document => document.UpdatedAtUtc).IsRequired();
            entity.HasMany(document => document.Chunks)
                .WithOne(chunk => chunk.Document)
                .HasForeignKey(chunk => chunk.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RunbookChunkEntity>(entity =>
        {
            entity.ToTable("Chunks");
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.Id).ValueGeneratedNever();
            entity.Property(chunk => chunk.DocumentId).IsRequired();
            entity.Property(chunk => chunk.DocumentVersion).IsRequired();
            entity.Property(chunk => chunk.Ordinal).IsRequired();
            entity.Property(chunk => chunk.Text).IsRequired();
            entity.Property(chunk => chunk.Heading);
            entity.Property(chunk => chunk.StartLine).IsRequired();
            entity.Property(chunk => chunk.EndLine).IsRequired();
            entity.HasIndex(chunk => new { chunk.DocumentId, chunk.DocumentVersion, chunk.Ordinal })
                .IsUnique();
            entity.HasOne(chunk => chunk.CitationReference)
                .WithOne(reference => reference.Chunk)
                .HasForeignKey<RunbookCitationReferenceEntity>(reference => reference.ChunkId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RunbookCitationReferenceEntity>(entity =>
        {
            entity.ToTable("CitationReferences");
            entity.HasKey(reference => reference.ChunkId);
            entity.Property(reference => reference.ChunkId).ValueGeneratedNever();
            entity.Property(reference => reference.DocumentId).IsRequired();
            entity.Property(reference => reference.DocumentVersion).IsRequired();
            entity.Property(reference => reference.Heading);
            entity.Property(reference => reference.StartLine).IsRequired();
            entity.Property(reference => reference.EndLine).IsRequired();
        });
    }
}
