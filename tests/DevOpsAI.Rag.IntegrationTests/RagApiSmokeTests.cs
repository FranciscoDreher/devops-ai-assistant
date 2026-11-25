using System.Net;
using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Documents;
using DevOpsAI.Rag.Application.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsAI.Rag.IntegrationTests;

public class RagApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RagApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpointShouldReturnHealthyAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("healthy", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultHostUrlBindsToLoopback()
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        Assert.Equal("http://127.0.0.1:5082", configuration["Urls"]);
    }

    [Fact]
    public void RunbookDocumentValidatorUsesConfiguredMaximumFileSize()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{RunbookDocumentOptions.SectionName}:MaxDocumentSizeBytes"] = "4"
                })));
        var validator = factory.Services.GetRequiredService<RunbookDocumentValidator>();

        var exception = Assert.Throws<RunbookDocumentValidationException>(
            () => validator.Validate("runbook", "runbook.md", new byte[5]));

        Assert.Equal(RunbookDocumentValidationErrorCode.DocumentTooLarge, exception.ErrorCode);
    }

    [Fact]
    public async Task GroundedAnswerDependenciesResolveWithConfiguredModelsAndCopiedPrompts()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ollama:ModelId"] = "test-embedding-model",
                    ["Ollama:Chat:ModelId"] = "test-chat-model"
                })));
        await using var scope = factory.Services.CreateAsyncScope();

        var generateAnswer = scope.ServiceProvider.GetRequiredService<GenerateRunbookAnswer>();
        var promptProvider = scope.ServiceProvider.GetRequiredService<IRunbookPromptProvider>();
        var prompt = await promptProvider.GetPromptAsync(CancellationToken.None);

        Assert.NotNull(generateAnswer);
        Assert.Contains("supplied retrieved runbook passages", prompt.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("citedChunkIds", prompt.AnswerFormatPrompt, StringComparison.Ordinal);
    }
}
