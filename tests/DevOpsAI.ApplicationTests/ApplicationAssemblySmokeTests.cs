using System.Reflection;
using DevOpsAI.Application;

namespace DevOpsAI.ApplicationTests;

public class ApplicationAssemblySmokeTests
{
    [Fact]
    public void ApplicationAssemblyShouldHaveExpectedName()
    {
        var assemblyName = typeof(ProjectMarker).Assembly.GetName().Name;

        Assert.Equal(ProjectMarker.ProjectName, assemblyName);
    }

    [Fact]
    public void ApplicationAssemblyShouldNotReferenceInfrastructure()
    {
        var applicationAssembly = typeof(ProjectMarker).Assembly;
        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencedAssemblies, assembly => assembly.Name == "DevOpsAI.Infrastructure");
    }
}
