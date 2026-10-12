using DevOpsAI.Application;

namespace DevOpsAI.IntegrationTests.Fakes;

public sealed class FakeAiTextGenerator(string response, Exception? exceptionToThrow = null) : IAiTextGenerator
{
    public Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (exceptionToThrow is not null)
        {
            throw exceptionToThrow;
        }

        return Task.FromResult(response);
    }
}
