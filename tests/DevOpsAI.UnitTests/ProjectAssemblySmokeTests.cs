using System.Reflection;
using DevOpsAI.Domain;

namespace DevOpsAI.UnitTests;

public class ProjectAssemblySmokeTests
{
    [Fact]
    public void DomainAssemblyShouldHaveExpectedName()
    {
        var assemblyName = typeof(ProjectMarker).Assembly.GetName().Name;

        Assert.Equal(ProjectMarker.ProjectName, assemblyName);
    }

    [Fact]
    public void DomainAssemblyShouldNotReferenceInfrastructureOrAspNet()
    {
        var domainAssembly = typeof(ProjectMarker).Assembly;
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencedAssemblies, assembly => assembly.Name == "DevOpsAI.Infrastructure");
        Assert.DoesNotContain(referencedAssemblies, assembly => assembly.Name == "Microsoft.AspNetCore.App");
    }
}
