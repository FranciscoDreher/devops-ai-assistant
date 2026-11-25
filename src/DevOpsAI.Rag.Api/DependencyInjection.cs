using DevOpsAI.Rag.Api.ErrorHandling;
using DevOpsAI.Rag.Application.Documents;

namespace DevOpsAI.Rag.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddRagDocumentApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails();
        services.AddExceptionHandler<RagApiExceptionHandler>();
        services.AddScoped<ReplaceRunbookDocument>();
        services.AddScoped<DeleteRunbookDocument>();
        services.AddScoped<ListRunbookDocuments>();

        return services;
    }
}
