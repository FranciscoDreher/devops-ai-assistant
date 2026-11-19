using DevOpsAI.Rag.Application.Documents;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.ApplicationTests;

public class RunbookDocumentStoreContractTests
{
    [Fact]
    public void StorePortUsesDomainDocumentTypes()
    {
        var findMethod = typeof(IRunbookDocumentStore).GetMethod(nameof(IRunbookDocumentStore.FindByIdAsync));
        var saveMethod = typeof(IRunbookDocumentStore).GetMethod(nameof(IRunbookDocumentStore.SaveAsync));
        var deleteMethod = typeof(IRunbookDocumentStore).GetMethod(nameof(IRunbookDocumentStore.DeleteAsync));
        var listMethod = typeof(IRunbookDocumentStore).GetMethod(nameof(IRunbookDocumentStore.ListAsync));

        Assert.NotNull(findMethod);
        Assert.NotNull(saveMethod);
        Assert.NotNull(deleteMethod);
        Assert.NotNull(listMethod);
        Assert.Equal(typeof(Task<KnowledgeDocument?>), findMethod.ReturnType);
        Assert.Equal(typeof(KnowledgeDocument), saveMethod.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), saveMethod.GetParameters()[1].ParameterType);
        Assert.Equal(typeof(KnowledgeDocument), deleteMethod.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), deleteMethod.GetParameters()[1].ParameterType);
        Assert.Equal(typeof(Task<IReadOnlyList<RunbookDocumentSummary>>), listMethod.ReturnType);
        Assert.Equal(typeof(CancellationToken), listMethod.GetParameters()[0].ParameterType);
    }
}
