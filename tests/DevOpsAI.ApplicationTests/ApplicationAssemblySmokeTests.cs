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
}
