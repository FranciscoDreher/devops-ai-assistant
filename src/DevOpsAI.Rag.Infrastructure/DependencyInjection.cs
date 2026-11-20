using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Infrastructure.Embeddings;
using DevOpsAI.Rag.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace DevOpsAI.Rag.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRagInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var sqliteConnectionString = new SqliteConnectionStringBuilder(connectionString)
        {
            ForeignKeys = true
        }.ToString();

        services.AddDbContext<RagDbContext>(options => options.UseSqlite(sqliteConnectionString));
        services.AddScoped<IRunbookDocumentStore, SqliteRunbookDocumentStore>();
        services.AddScoped<IRunbookKnowledgeIndex, SqliteRunbookKnowledgeIndex>();
        services.AddSingleton<HttpClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OllamaEmbeddingOptions>>().Value;
            options.Validate();
            return new HttpClient
            {
                BaseAddress = new Uri(options.Endpoint),
                Timeout = options.RequestTimeout
            };
        });
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OllamaEmbeddingOptions>>().Value;
            return new OllamaApiClient(serviceProvider.GetRequiredService<HttpClient>(), options.ModelId);
        });
        services.AddSingleton<ITextEmbeddingGenerator, OllamaTextEmbeddingGenerator>();

        return services;
    }

    public static async Task EnsureRagDatabaseCreatedAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var scope = serviceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RagDbContext>();
            var connectionString = dbContext.Database.GetConnectionString()
                ?? throw new InvalidOperationException("The RagDatabase connection string is not configured.");
            var sqliteConnectionString = new SqliteConnectionStringBuilder(connectionString);

            if (sqliteConnectionString.Mode != SqliteOpenMode.Memory
                && !string.IsNullOrWhiteSpace(sqliteConnectionString.DataSource))
            {
                var databasePath = Path.GetFullPath(sqliteConnectionString.DataSource);
                var directoryPath = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }

            await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "ChunkEmbeddings" (
                    "ChunkId" TEXT NOT NULL PRIMARY KEY,
                    "ModelId" TEXT NOT NULL,
                    "Dimensions" INTEGER NOT NULL CHECK ("Dimensions" > 0),
                    "VectorBlob" BLOB NOT NULL CHECK (length("VectorBlob") = "Dimensions" * 4),
                    FOREIGN KEY ("ChunkId") REFERENCES "Chunks" ("Id") ON DELETE CASCADE
                )
                """,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
