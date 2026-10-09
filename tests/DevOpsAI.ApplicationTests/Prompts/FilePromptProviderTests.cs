using DevOpsAI.Application;
using DevOpsAI.Domain;
using DevOpsAI.Infrastructure.Prompts;

namespace DevOpsAI.ApplicationTests.Prompts;

public class FilePromptProviderTests
{
    [Fact]
    public async Task GetAudiencePromptAsyncForLeadershipLoadsLeadershipPromptFromFile()
    {
        var promptRoot = CreatePromptRoot();
        var provider = new FilePromptProvider(promptRoot);

        var prompt = await provider.GetAudiencePromptAsync(IncidentAudience.Leadership, CancellationToken.None);

        Assert.Equal("Leadership prompt content.", prompt);
    }

    [Fact]
    public async Task GetAudiencePromptAsyncForCustomerLoadsCustomerPromptFromFile()
    {
        var promptRoot = CreatePromptRoot();
        var provider = new FilePromptProvider(promptRoot);

        var prompt = await provider.GetAudiencePromptAsync(IncidentAudience.Customer, CancellationToken.None);

        Assert.Equal("Customer prompt content.", prompt);
    }

    [Fact]
    public async Task GetAudiencePromptAsyncForUnsupportedAudienceThrowsControlledFailure()
    {
        var promptRoot = CreatePromptRoot();
        var provider = new FilePromptProvider(promptRoot);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            provider.GetAudiencePromptAsync((IncidentAudience)999, CancellationToken.None));
    }

    [Fact]
    public async Task DefaultConstructorResolvesPromptRootFromCurrentDirectory()
    {
        var originalCurrentDirectory = Environment.CurrentDirectory;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"devops-ai-prompt-default-{Guid.NewGuid():N}");
        var projectRoot = Path.Combine(tempRoot, "repo");
        var promptRoot = Path.Combine(projectRoot, "docs", "prompts", "incident-polisher");
        Directory.CreateDirectory(promptRoot);

        File.WriteAllText(Path.Combine(promptRoot, "system.md"), "System prompt content.");
        File.WriteAllText(Path.Combine(promptRoot, "leadership.md"), "Leadership prompt content.");
        File.WriteAllText(Path.Combine(promptRoot, "customer.md"), "Customer prompt content.");

        try
        {
            Environment.CurrentDirectory = projectRoot;

            var provider = new FilePromptProvider();
            var prompt = await provider.GetSystemPromptAsync(CancellationToken.None);

            Assert.Equal("System prompt content.", prompt);
        }
        finally
        {
            Environment.CurrentDirectory = originalCurrentDirectory;
        }
    }

    private static string CreatePromptRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"devops-ai-prompt-tests-{Guid.NewGuid():N}");
        var folder = Path.Combine(root, "incident-polisher");
        Directory.CreateDirectory(folder);

        File.WriteAllText(Path.Combine(folder, "system.md"), "System prompt content.");
        File.WriteAllText(Path.Combine(folder, "leadership.md"), "Leadership prompt content.");
        File.WriteAllText(Path.Combine(folder, "customer.md"), "Customer prompt content.");

        return folder;
    }
}
