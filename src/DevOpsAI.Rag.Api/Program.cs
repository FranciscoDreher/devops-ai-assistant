using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Documents;
using DevOpsAI.Rag.Infrastructure;
using DevOpsAI.Rag.Infrastructure.Embeddings;
using DevOpsAI.Rag.Infrastructure.Generation;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<OllamaEmbeddingOptions>(
    builder.Configuration.GetSection(OllamaEmbeddingOptions.SectionName));
builder.Services.Configure<OllamaChatOptions>(
    builder.Configuration.GetSection(OllamaChatOptions.SectionName));
builder.Services.Configure<RunbookRetrievalOptions>(
    builder.Configuration.GetSection(RunbookRetrievalOptions.SectionName));
var databaseConnectionString = builder.Configuration.GetConnectionString("RagDatabase")
    ?? throw new InvalidOperationException("The RagDatabase connection string is required.");
builder.Services.AddRagInfrastructure(databaseConnectionString);
builder.Services.Configure<RunbookDocumentOptions>(
    builder.Configuration.GetSection(RunbookDocumentOptions.SectionName));
builder.Services.Configure<RunbookChunkingOptions>(
    builder.Configuration.GetSection(RunbookChunkingOptions.SectionName));
builder.Services.AddSingleton<RunbookDocumentValidator>();
builder.Services.AddSingleton<RunbookDocumentChunker>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<ReplaceRunbookDocument>();
builder.Services.AddScoped<GenerateRunbookAnswer>();

var app = builder.Build();

await app.Services.EnsureRagDatabaseCreatedAsync().ConfigureAwait(false);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet(
    "/ready",
    (IOptions<OllamaEmbeddingOptions> embeddingOptions, IOptions<OllamaChatOptions> chatOptions) =>
    {
        var missingConfiguration = new List<string>();
        if (string.IsNullOrWhiteSpace(embeddingOptions.Value.ModelId))
        {
            missingConfiguration.Add($"{OllamaEmbeddingOptions.SectionName}:ModelId");
        }

        if (string.IsNullOrWhiteSpace(chatOptions.Value.ModelId))
        {
            missingConfiguration.Add($"{OllamaChatOptions.SectionName}:ModelId");
        }

        return missingConfiguration.Count > 0
            ? Results.Json(
                new { status = "not_ready", missingConfiguration },
                statusCode: StatusCodes.Status503ServiceUnavailable)
            : Results.Ok(new { status = "ready" });
    });

app.Run();

public partial class Program
{
}
