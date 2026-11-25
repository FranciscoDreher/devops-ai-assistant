using DevOpsAI.Rag.Application.Chat;

namespace DevOpsAI.Rag.Application.Ports;

public interface IRunbookPromptProvider
{
    Task<RunbookPromptInstructions> GetPromptAsync(CancellationToken cancellationToken);
}
