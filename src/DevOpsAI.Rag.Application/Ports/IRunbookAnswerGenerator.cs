using DevOpsAI.Rag.Application.Chat;

namespace DevOpsAI.Rag.Application.Ports;

public interface IRunbookAnswerGenerator
{
    Task<GeneratedRunbookAnswer> GenerateAsync(
        RunbookAnswerPrompt prompt,
        CancellationToken cancellationToken);
}
