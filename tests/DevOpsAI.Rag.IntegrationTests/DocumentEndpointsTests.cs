using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.IntegrationTests;

public sealed class DocumentEndpointsTests
{
    [Fact]
    public async Task OpenApiDocumentDescribesDocumentManagementRoutesAsync()
    {
        using var host = new ApiTestHost();

        using var response = await host.Client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/documents", document, StringComparison.Ordinal);
        Assert.Contains("/api/documents/{documentId}", document, StringComparison.Ordinal);
        Assert.Contains("multipart/form-data", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PutCreatesAndReplacesDocumentAndListReturnsCurrentMetadataAsync()
    {
        using var host = new ApiTestHost();

        using var createResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            "# Recovery\nRestart the cache safely.");

        Assert.True(
            createResponse.StatusCode == HttpStatusCode.Created,
            await createResponse.Content.ReadAsStringAsync());
        Assert.Equal("/api/documents/cache-runbook", createResponse.Headers.Location?.OriginalString);

        using var created = await ReadJsonAsync(createResponse);
        var createdDocument = created.RootElement;
        Assert.Equal("cache-runbook", createdDocument.GetProperty("documentId").GetString());
        Assert.Equal("cache-recovery.md", createdDocument.GetProperty("fileName").GetString());
        Assert.Equal("indexed", createdDocument.GetProperty("status").GetString());
        Assert.False(createdDocument.TryGetProperty("content", out _));
        Assert.Equal(1, createdDocument.GetProperty("version").GetInt32());
        Assert.True(createdDocument.GetProperty("chunkCount").GetInt32() > 0);
        var originalHash = createdDocument.GetProperty("contentHash").GetString();

        using var replaceResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            "# Recovery\nRestart and verify the cache.");

        Assert.Equal(HttpStatusCode.OK, replaceResponse.StatusCode);

        using var replaced = await ReadJsonAsync(replaceResponse);
        var replacedDocument = replaced.RootElement;
        Assert.Equal(2, replacedDocument.GetProperty("version").GetInt32());
        Assert.NotEqual(originalHash, replacedDocument.GetProperty("contentHash").GetString());

        using var listResponse = await host.Client.GetAsync("/api/documents");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var list = await ReadJsonAsync(listResponse);
        var listedDocument = Assert.Single(list.RootElement.GetProperty("documents").EnumerateArray());
        Assert.Equal(2, listedDocument.GetProperty("version").GetInt32());
        Assert.Equal(replacedDocument.GetProperty("contentHash").GetString(),
            listedDocument.GetProperty("contentHash").GetString());
        Assert.Equal("indexed", listedDocument.GetProperty("status").GetString());
        Assert.False(listedDocument.TryGetProperty("content", out _));
    }

    [Fact]
    public async Task PutRejectsInvalidContentWithoutReplacingPreviousVersionAsync()
    {
        using var host = new ApiTestHost();
        using var createResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            "Original valid runbook content.");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var invalidResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        using var error = await ReadJsonAsync(invalidResponse);
        Assert.Equal("INVALID_DOCUMENT_CONTENT", error.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.RootElement.GetProperty("traceId").GetString()));

        using var listResponse = await host.Client.GetAsync("/api/documents");
        using var list = await ReadJsonAsync(listResponse);
        var document = Assert.Single(list.RootElement.GetProperty("documents").EnumerateArray());
        Assert.Equal(1, document.GetProperty("version").GetInt32());
        Assert.Equal("indexed", document.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PutReturnsExplicitErrorsForMissingFileUnsupportedFormatAndOversizedContentAsync()
    {
        using var host = new ApiTestHost();

        using var missingFileContent = new MultipartFormDataContent();
        using var missingFileResponse = await host.Client.PutAsync(
            "/api/documents/cache-runbook",
            missingFileContent);
        Assert.Equal(HttpStatusCode.BadRequest, missingFileResponse.StatusCode);

        using var unsupportedResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.pdf",
            "Unsupported format.");
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupportedResponse.StatusCode);

        using var oversizedResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            new string('x', 70_000));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversizedResponse.StatusCode);

        using var listResponse = await host.Client.GetAsync("/api/documents");
        using var list = await ReadJsonAsync(listResponse);
        Assert.Empty(list.RootElement.GetProperty("documents").EnumerateArray());
    }

    [Fact]
    public async Task PutReturnsValidationErrorsForInvalidIdentifiersAndInvalidUtf8Async()
    {
        using var host = new ApiTestHost();

        using var invalidIdResponse = await PutDocumentAsync(
            host.Client,
            "bad%5Cpath",
            "cache-recovery.md",
            "Valid document content.");
        Assert.Equal(HttpStatusCode.BadRequest, invalidIdResponse.StatusCode);
        using var invalidIdError = await ReadJsonAsync(invalidIdResponse);
        Assert.Equal("VALIDATION_ERROR", invalidIdError.RootElement.GetProperty("code").GetString());

        using var invalidContentResponse = await PutDocumentBytesAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            [0xff]);
        Assert.Equal(HttpStatusCode.BadRequest, invalidContentResponse.StatusCode);
        using var invalidContentError = await ReadJsonAsync(invalidContentResponse);
        Assert.Equal("INVALID_DOCUMENT_CONTENT", invalidContentError.RootElement.GetProperty("code").GetString());

        using var listResponse = await host.Client.GetAsync("/api/documents");
        using var list = await ReadJsonAsync(listResponse);
        Assert.Empty(list.RootElement.GetProperty("documents").EnumerateArray());
    }

    [Fact]
    public async Task DeleteReportsMissingDocumentAndReflectsTombstoneInListAsync()
    {
        using var host = new ApiTestHost();

        using var missingResponse = await host.Client.DeleteAsync("/api/documents/missing");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        using var missingError = await ReadJsonAsync(missingResponse);
        Assert.Equal("DOCUMENT_NOT_FOUND", missingError.RootElement.GetProperty("code").GetString());

        using var createResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            "The active document has searchable content.");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var deleteResponse = await host.Client.DeleteAsync("/api/documents/cache-runbook");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var listResponse = await host.Client.GetAsync("/api/documents");
        using var list = await ReadJsonAsync(listResponse);
        var deletedDocument = Assert.Single(list.RootElement.GetProperty("documents").EnumerateArray());
        Assert.Equal("deleted", deletedDocument.GetProperty("status").GetString());
        Assert.Equal(0, deletedDocument.GetProperty("chunkCount").GetInt32());
    }

    [Fact]
    public async Task PutReturnsSafeTechnicalErrorAndKeepsPreviousVersionWhenEmbeddingFailsAsync()
    {
        using var host = new ApiTestHost();
        using var createResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            "Original valid runbook content.");
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var failureResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            "trigger embedding failure");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, failureResponse.StatusCode);
        var errorBody = await failureResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("sensitive provider detail", errorBody, StringComparison.Ordinal);
        using var error = JsonDocument.Parse(errorBody);
        Assert.Equal("DEPENDENCY_UNAVAILABLE", error.RootElement.GetProperty("code").GetString());

        using var internalFailureResponse = await PutDocumentAsync(
            host.Client,
            "cache-runbook",
            "cache-recovery.md",
            "trigger internal failure");
        Assert.Equal(HttpStatusCode.InternalServerError, internalFailureResponse.StatusCode);
        var internalErrorBody = await internalFailureResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("sensitive internal details", internalErrorBody, StringComparison.Ordinal);
        using var internalError = JsonDocument.Parse(internalErrorBody);
        Assert.Equal("INTERNAL_ERROR", internalError.RootElement.GetProperty("code").GetString());

        using var listResponse = await host.Client.GetAsync("/api/documents");
        using var list = await ReadJsonAsync(listResponse);
        var document = Assert.Single(list.RootElement.GetProperty("documents").EnumerateArray());
        Assert.Equal(1, document.GetProperty("version").GetInt32());
    }

    private static async Task<HttpResponseMessage> PutDocumentAsync(
        HttpClient client,
        string documentId,
        string fileName,
        string content)
    {
        return await PutDocumentBytesAsync(
            client,
            documentId,
            fileName,
            Encoding.UTF8.GetBytes(content)).ConfigureAwait(false);
    }

    private static async Task<HttpResponseMessage> PutDocumentBytesAsync(
        HttpClient client,
        string documentId,
        string fileName,
        byte[] content)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        return await client.PutAsync($"/api/documents/{documentId}", form).ConfigureAwait(false);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonDocument.Parse(content);
    }

    private sealed class ApiTestHost : IDisposable
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"rag-api-{Guid.NewGuid():N}.db");
        private readonly WebApplicationFactory<Program> _factory;

        public ApiTestHost()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<RagDbContext>();
                    services.RemoveAll<DbContextOptions<RagDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<RagDbContext>>();
                    services.AddDbContext<RagDbContext>(options =>
                        options.UseSqlite($"Data Source={_databasePath};Foreign Keys=True;Pooling=False"));
                    services.RemoveAll<IOptions<RunbookDocumentOptions>>();
                    services.AddSingleton<IOptions<RunbookDocumentOptions>>(
                        Options.Create(new RunbookDocumentOptions { MaxDocumentSizeBytes = 128 }));
                    services.RemoveAll<ITextEmbeddingGenerator>();
                    services.AddSingleton<ITextEmbeddingGenerator, TestTextEmbeddingGenerator>();
                });
            });

            Client = _factory.CreateClient();
            using var scope = _factory.Services.CreateScope();
            var actualConnectionString = scope.ServiceProvider
                .GetRequiredService<RagDbContext>()
                .Database
                .GetConnectionString();
            Assert.Contains(_databasePath, actualConnectionString, StringComparison.Ordinal);
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            _factory.Dispose();
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
    }

    private sealed class TestTextEmbeddingGenerator : ITextEmbeddingGenerator
    {
        private static readonly float[] Embedding = [1f, 0f, 0f];

        public string ModelId => "offline-test-model";

        public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (text.Contains("trigger embedding failure", StringComparison.Ordinal))
            {
                throw new HttpRequestException("sensitive provider detail");
            }

            if (text.Contains("trigger internal failure", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("sensitive internal details");
            }

            return Task.FromResult<ReadOnlyMemory<float>>(Embedding);
        }
    }
}
