using System.Xml.Linq;

namespace DevOpsAI.Rag.UnitTests;

public class RagProjectBoundaryTests
{
    private static readonly string[] ProductionProjects =
    [
        "DevOpsAI.Rag.Domain",
        "DevOpsAI.Rag.Application",
        "DevOpsAI.Rag.Infrastructure",
        "DevOpsAI.Rag.Api"
    ];

    [Fact]
    public void ProjectReferencesFollowCleanArchitecture()
    {
        Assert.Empty(GetProjectReferences("DevOpsAI.Rag.Domain"));
        AssertReferences("DevOpsAI.Rag.Application", "DevOpsAI.Rag.Domain");
        AssertReferences(
            "DevOpsAI.Rag.Infrastructure",
            "DevOpsAI.Rag.Application",
            "DevOpsAI.Rag.Domain");
        AssertReferences(
            "DevOpsAI.Rag.Api",
            "DevOpsAI.Rag.Application",
            "DevOpsAI.Rag.Infrastructure");
    }

    [Fact]
    public void Project2DoesNotReferenceProject1Assemblies()
    {
        foreach (var project in ProductionProjects)
        {
            var project1References = GetProjectReferences(project)
                .Where(reference => reference.StartsWith("DevOpsAI.", StringComparison.Ordinal))
                .Where(reference => !reference.StartsWith("DevOpsAI.Rag.", StringComparison.Ordinal));

            Assert.Empty(project1References);
        }
    }

    [Fact]
    public void DomainAndApplicationDoNotReferenceInfrastructureOrProviderPackages()
    {
        var forbiddenPackages = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Extensions.AI",
            "Microsoft.Data.Sqlite",
            "Azure.",
            "OpenAI",
            "OllamaSharp",
            "Qdrant.Client"
        };

        foreach (var project in new[] { "DevOpsAI.Rag.Domain", "DevOpsAI.Rag.Application" })
        {
            var forbiddenReferences = GetPackageReferences(project)
                .Where(package => forbiddenPackages.Any(
                    forbidden => package.StartsWith(forbidden, StringComparison.Ordinal)));

            Assert.Empty(forbiddenReferences);
        }
    }

    private static void AssertReferences(string project, params string[] expectedReferences)
    {
        var actualReferences = GetProjectReferences(project)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedReferences.OrderBy(reference => reference, StringComparer.Ordinal), actualReferences);
    }

    private static string[] GetProjectReferences(string projectName)
    {
        return GetProjectFile(projectName)
            .Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .OfType<string>()
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .ToArray();
    }

    private static string[] GetPackageReferences(string projectName)
    {
        return GetProjectFile(projectName)
            .Descendants("PackageReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .OfType<string>()
            .ToArray();
    }

    private static XDocument GetProjectFile(string projectName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot, "src", projectName, $"{projectName}.csproj");

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file '{projectName}.csproj' was not found.", projectPath);
        }

        return XDocument.Load(projectPath);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "devops-ai-assistant.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("The repository root was not found from the test output directory.");
    }
}
