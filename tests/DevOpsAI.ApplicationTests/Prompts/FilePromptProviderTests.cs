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
