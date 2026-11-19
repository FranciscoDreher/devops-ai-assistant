using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        }
    }
}
