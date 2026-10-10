using DevOpsAI.Application;
using DevOpsAI.Infrastructure.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DevOpsAI.IntegrationTests.AI;

public class MicrosoftExtensionsAiTextGeneratorTests
{
    [Fact]
    public async Task GenerateAsyncReturnsTheProviderText()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"summary\":\"Database latency recovered\"}"))));

        var generator = new MicrosoftExtensionsAiTextGenerator(
            chatClient,
            Options.Create(new AiOptions
            {
                Model = "gpt-4o-mini",
                Endpoint = "https://example.test",
                ApiKey = "test-key"
            }));

        var result = await generator.GenerateAsync(
            "system instructions",
            "user prompt",
            CancellationToken.None);

        Assert.Equal("{\"summary\":\"Database latency recovered\"}", result);
        await chatClient.Received(1).GetResponseAsync(
            Arg.Is<IEnumerable<ChatMessage>>(messages =>
                messages.Any(message => message.Role == ChatRole.System && message.Text.Contains("system instructions", StringComparison.Ordinal)) &&
                messages.Any(message => message.Role == ChatRole.User && message.Text.Contains("user prompt", StringComparison.Ordinal))),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsyncWhenProviderFailsThrowsStructuredInfrastructureException()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(new InvalidOperationException("provider unavailable")));

        var generator = new MicrosoftExtensionsAiTextGenerator(
            chatClient,
            Options.Create(new AiOptions
            {
                Model = "gpt-4o-mini",
                Endpoint = "https://example.test",
                ApiKey = "test-key"
            }));

        var ex = await Assert.ThrowsAsync<AiGenerationException>(() =>
            generator.GenerateAsync("system instructions", "user prompt", CancellationToken.None));

        Assert.Contains("provider", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsyncWhenProviderReturnsPlainTextRejectsIt()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "plain text response"))));

        var generator = new MicrosoftExtensionsAiTextGenerator(
            chatClient,
            Options.Create(new AiOptions
            {
                Model = "gpt-4o-mini",
                Endpoint = "https://example.test",
                ApiKey = "test-key"
            }));

        var ex = await Assert.ThrowsAsync<AiGenerationException>(() =>
            generator.GenerateAsync("system instructions", "user prompt", CancellationToken.None));

        Assert.Contains("JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
