using DevOpsAI.Domain;

namespace DevOpsAI.Application;

public interface IPromptProvider
{
    Task<string> GetSystemPromptAsync(CancellationToken cancellationToken);

    Task<string> GetAudiencePromptAsync(
        IncidentAudience audience,
        CancellationToken cancellationToken);
}
