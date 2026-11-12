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

        Assert.NotNull(findMethod);
        Assert.NotNull(saveMethod);
        Assert.Equal(typeof(Task<KnowledgeDocument?>), findMethod.ReturnType);
        Assert.Equal(typeof(KnowledgeDocument), saveMethod.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), saveMethod.GetParameters()[1].ParameterType);
    }
}
