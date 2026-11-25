using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Infrastructure.Generation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DevOpsAI.Rag.UnitTests.Generation;

public sealed class OllamaRunbookAnswerGeneratorTests
{
    [Fact]
    public async Task GenerateAsyncWhenStructuredAnswerIsValidReturnsParsedAnswer()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                """{"status":"answered","answer":"Restart the cache service.","citedChunkIds":["chunk-1"]}""")));
        var generator = CreateGenerator(chatClient);
        var prompt = CreatePrompt();

        var result = await generator.GenerateAsync(prompt, CancellationToken.None);

        Assert.Equal(RunbookAnswerStatus.Answered, result.Status);
        Assert.Equal("Restart the cache service.", result.Answer);
        Assert.Equal("chunk-1", Assert.Single(result.CitedChunkIds));
        await chatClient.Received(1).GetResponseAsync(
            Arg.Is<IEnumerable<ChatMessage>>(messages =>
                messages.Any(message =>
                    message.Role == ChatRole.System
                    && message.Text.Contains("Answer only from supplied evidence.", StringComparison.Ordinal)
                    && message.Text.Contains("Return the required JSON.", StringComparison.Ordinal))
                && messages.Any(message =>
                    message.Role == ChatRole.User
                    && message.Text.Contains("How do I recover the cache?", StringComparison.Ordinal)
                    && message.Text.Contains("chunk-1", StringComparison.Ordinal)
                    && message.Text.Contains("Restart the cache service.", StringComparison.Ordinal))),
            Arg.Is<ChatOptions>(options => options.ModelId == "local-chat-model" && options.Temperature == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsyncWhenProviderAbstainsReturnsNoCitations()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                """{"status":"insufficient_evidence","answer":"There is not enough information.","citedChunkIds":[]}""")));
        var generator = CreateGenerator(chatClient);

        var result = await generator.GenerateAsync(CreatePrompt(), CancellationToken.None);

        Assert.Equal(RunbookAnswerStatus.InsufficientEvidence, result.Status);
        Assert.Empty(result.CitedChunkIds);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"status":"answered","answer":"answer","citedChunkIds":["chunk-1"],"source":"invented"}""")]
    [InlineData("""{"status":"unknown","answer":"answer","citedChunkIds":["chunk-1"]}""")]
    [InlineData("""{"status":"answered","answer":"answer","citedChunkIds":[]}""")]
    [InlineData("""{"status":"insufficient_evidence","answer":"not enough","citedChunkIds":["chunk-1"]}""")]
    [InlineData("""{"status":"answered","status":"insufficient_evidence","answer":"not enough","citedChunkIds":[]}""")]
    public async Task GenerateAsyncWhenProviderReturnsInvalidStructuredAnswerThrowsInvalidDataException(
        string providerOutput)
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, providerOutput)));
        var generator = CreateGenerator(chatClient);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            generator.GenerateAsync(CreatePrompt(), CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsyncWhenProviderResponseIsEmptyThrowsInvalidDataException()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, " ")));
        var generator = CreateGenerator(chatClient);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            generator.GenerateAsync(CreatePrompt(), CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsyncPropagatesCancellationToChatClient()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromCanceled<ChatResponse>(callInfo.Arg<CancellationToken>()));
        var generator = CreateGenerator(chatClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            generator.GenerateAsync(CreatePrompt(), cancellationSource.Token));

        await chatClient.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            cancellationSource.Token);
    }

    private static OllamaRunbookAnswerGenerator CreateGenerator(IChatClient chatClient)
    {
        return new OllamaRunbookAnswerGenerator(
            chatClient,
            Options.Create(new OllamaChatOptions { ModelId = " local-chat-model " }));
    }

    private static RunbookAnswerPrompt CreatePrompt()
    {
        return new RunbookAnswerPrompt(
            new RunbookPromptInstructions("Answer only from supplied evidence.", "Return the required JSON."),
            "How do I recover the cache?",
            [new RunbookPromptEvidence("chunk-1", "Restart the cache service.")]);
    }
}
