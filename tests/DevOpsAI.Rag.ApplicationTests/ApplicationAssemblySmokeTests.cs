using DevOpsAI.Rag.Application;

namespace DevOpsAI.Rag.ApplicationTests;

public class ApplicationAssemblySmokeTests
{
    [Fact]
    public void ApplicationAssemblyShouldHaveExpectedName()
    {
        var assemblyName = typeof(ProjectMarker).Assembly.GetName().Name;

        Assert.Equal(ProjectMarker.ProjectName, assemblyName);
    }

    [Fact]
    public void ApplicationAssemblyShouldNotReferenceInfrastructureOrProviderSdk()
    {
        var referencedAssemblies = typeof(ProjectMarker).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencedAssemblies, assembly =>
            assembly.Name is "DevOpsAI.Rag.Infrastructure"
                or "DevOpsAI.Infrastructure"
                or "Microsoft.Extensions.AI"
                or "Microsoft.EntityFrameworkCore"
                or "OllamaSharp");
    }
}
