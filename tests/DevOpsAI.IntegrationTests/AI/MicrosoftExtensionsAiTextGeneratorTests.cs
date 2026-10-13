using System.Diagnostics;
using DevOpsAI.Application;
using DevOpsAI.Infrastructure.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        const string secret = "provider-secret-marker";
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(new InvalidOperationException($"provider unavailable: {secret}")));

        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider));
        var generator = new MicrosoftExtensionsAiTextGenerator(
            chatClient,
            Options.Create(new AiOptions
            {
                Model = "gpt-4o-mini",
                Endpoint = "https://example.test",
                ApiKey = "test-key"
            }),
            loggerFactory.CreateLogger<MicrosoftExtensionsAiTextGenerator>());

        var ex = await Assert.ThrowsAsync<AiGenerationException>(() =>
            generator.GenerateAsync("system instructions", "user prompt", CancellationToken.None));

        Assert.DoesNotContain("provider unavailable", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(ex.InnerException);
        Assert.DoesNotContain(secret, string.Join(Environment.NewLine, loggerProvider.Messages), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsyncRecordsLatencyWithoutLoggingPromptsOrSecrets()
    {
        const string secret = "sensitive-api-token";
        const string prompt = "incident details must not be logged";
        var chatClient = CreateSuccessfulChatClient();
        using var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(loggerProvider));
        using var activity = new Activity("test-ai-generation").SetIdFormat(ActivityIdFormat.W3C).Start();
        var generator = new MicrosoftExtensionsAiTextGenerator(
            chatClient,
            Options.Create(new AiOptions
            {
                Model = "gpt-4o-mini",
                Endpoint = "https://example.test",
                ApiKey = secret
            }),
            loggerFactory.CreateLogger<MicrosoftExtensionsAiTextGenerator>());

        await generator.GenerateAsync("system instructions", prompt, CancellationToken.None);

        var log = string.Join(Environment.NewLine, loggerProvider.Messages);
        Assert.Contains("succeeded", log, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ElapsedMilliseconds", log, StringComparison.Ordinal);
        Assert.Contains($"TraceId={activity.TraceId}", log, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, log, StringComparison.Ordinal);
        Assert.DoesNotContain(prompt, log, StringComparison.Ordinal);
        Assert.DoesNotContain("system instructions", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsyncCancelsProviderCallWhenConfiguredTimeoutExpires()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call => WaitForCancellationAsync(call.ArgAt<CancellationToken>(2)));

        var generator = new MicrosoftExtensionsAiTextGenerator(
            chatClient,
            Options.Create(new AiOptions
            {
                Model = "gpt-4o-mini",
                Endpoint = "https://example.test",
                ApiKey = "test-key",
                TimeoutSeconds = 1
            }));

        var exception = await Assert.ThrowsAsync<AiGenerationException>(() =>
            generator.GenerateAsync("system instructions", "user prompt", CancellationToken.None));

        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
        await chatClient.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AddAiServicesBindsTimeoutConfiguration()
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:TimeoutSeconds"] = "7"
            })
            .Build();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        DevOpsAI.Infrastructure.AI.AiServiceCollectionExtensions.AddAiServices(services, configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal(7, options.TimeoutSeconds);
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

    [Fact]
    public void FromEnvironmentUsesDefaultTimeoutForMalformedValues()
    {
        var previousTimeout = Environment.GetEnvironmentVariable("AI_TIMEOUT_SECONDS");
        var previousDoubleUnderscoreTimeout = Environment.GetEnvironmentVariable("AI__TIMEOUT_SECONDS");

        try
        {
            Environment.SetEnvironmentVariable("AI_TIMEOUT_SECONDS", "not-a-number");
            Environment.SetEnvironmentVariable("AI__TIMEOUT_SECONDS", null);

            var options = AiOptions.FromEnvironment();

            Assert.Equal(AiOptions.DefaultTimeoutSeconds, options.TimeoutSeconds);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AI_TIMEOUT_SECONDS", previousTimeout);
            Environment.SetEnvironmentVariable("AI__TIMEOUT_SECONDS", previousDoubleUnderscoreTimeout);
        }
    }

    private static IChatClient CreateSuccessfulChatClient()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"summary\":\"ok\"}"))));

        return chatClient;
    }

    private static async Task<ChatResponse> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("The delay should only finish by cancellation.");
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages
        {
            get
            {
                lock (_messages)
                {
                    return _messages.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CapturingLogger(_messages);
        }

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (messages)
                {
                    messages.Add(formatter?.Invoke(state, exception) ?? string.Empty);
                    if (state is IEnumerable<KeyValuePair<string, object?>> properties)
                    {
                        messages.AddRange(properties.Select(property => $"{property.Key}={property.Value}"));
                    }
                }
            }
        }
    }
}
