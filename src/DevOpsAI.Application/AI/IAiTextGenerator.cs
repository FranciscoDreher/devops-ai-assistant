namespace DevOpsAI.Application;

public interface IAiTextGenerator
{
    Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken);
}
