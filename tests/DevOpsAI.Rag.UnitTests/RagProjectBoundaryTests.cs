using System.Xml.Linq;
using DevOpsAI.Rag.Domain.Documents;

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
            var project1References = GetTransitiveProjectReferences(project)
                .Where(reference => reference.StartsWith("DevOpsAI.", StringComparison.Ordinal))
                .Where(reference => !reference.StartsWith("DevOpsAI.Rag.", StringComparison.Ordinal));

            Assert.Empty(project1References);
        }
    }

    [Fact]
    public void DomainDoesNotDependOnInfrastructureTransitively()
    {
        Assert.DoesNotContain(
            "DevOpsAI.Rag.Infrastructure",
            GetTransitiveProjectReferences("DevOpsAI.Rag.Domain"));
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

    [Fact]
    public void DomainAssemblyDoesNotReferenceTransportPersistenceOrProviderFrameworks()
    {
        var forbiddenReferences = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Extensions.AI",
            "Microsoft.Data.Sqlite",
            "Azure.",
            "OpenAI",
            "OllamaSharp"
        };

        var references = typeof(KnowledgeDocument).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .OfType<string>();

        Assert.DoesNotContain(references, reference => forbiddenReferences.Any(
            forbidden => reference.StartsWith(forbidden, StringComparison.Ordinal)));
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

    private static string[] GetTransitiveProjectReferences(string projectName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var visitedProjects = new HashSet<string>(StringComparer.Ordinal);
        var pendingProjects = new Queue<string>();
        pendingProjects.Enqueue(projectName);

        while (pendingProjects.TryDequeue(out var currentProject))
        {
            if (!visitedProjects.Add(currentProject))
            {
                continue;
            }

            var projectPath = Path.Combine(
                repositoryRoot,
                "src",
                currentProject,
                $"{currentProject}.csproj");

            foreach (var reference in ReadProjectReferences(projectPath))
            {
                pendingProjects.Enqueue(reference);
            }
        }

        visitedProjects.Remove(projectName);
        return visitedProjects.ToArray();
    }

    private static string[] ReadProjectReferences(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path '{projectPath}' has no directory.");

        return XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .OfType<string>()
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)))
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
