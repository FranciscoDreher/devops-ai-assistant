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
}
