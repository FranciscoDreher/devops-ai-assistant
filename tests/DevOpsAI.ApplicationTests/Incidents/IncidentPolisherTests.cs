using DevOpsAI.Application;
using DevOpsAI.Domain;
using DevOpsAI.Infrastructure.Prompts;
using NSubstitute;

namespace DevOpsAI.ApplicationTests.Incidents;

public class IncidentPolisherTests
{
    [Fact]
    public async Task PolishAsyncWhenIncidentNoteIsEmptyReturnsValidationError()
    {
        var generator = Substitute.For<IAiTextGenerator>();
        var promptProvider = new FilePromptProvider(CreatePromptRoot());
        var polisher = new IncidentPolisher(generator, promptProvider);

        var result = await polisher.PolishAsync(
            new PolishIncidentRequest("   ", IncidentAudience.Leadership),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_INCIDENT_NOTE", result.Error!.Code);
        await generator.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default!, default);
    }

    [Fact]
    public async Task CreatePromptForLeadershipUsesLeadershipGuidance()
    {
        var promptProvider = new FilePromptProvider(CreatePromptRoot());

        var prompt = await IncidentPolishingRules.CreatePromptAsync(promptProvider, IncidentAudience.Leadership, "db latency issue");

        Assert.Equal("Leadership", prompt.AudienceName);
        Assert.Contains("executive briefing", prompt.UserPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("business impact", prompt.UserPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePromptForCustomerUsesCustomerGuidance()
    {
        var promptProvider = new FilePromptProvider(CreatePromptRoot());

        var prompt = await IncidentPolishingRules.CreatePromptAsync(promptProvider, IncidentAudience.Customer, "db latency issue");

        Assert.Equal("Customer", prompt.AudienceName);
        Assert.Contains("customer-facing", prompt.UserPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reassurance", prompt.UserPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PolishAsyncWhenIncidentNoteIsTooLongReturnsValidationError()
    {
        var generator = Substitute.For<IAiTextGenerator>();
        var promptProvider = new FilePromptProvider(CreatePromptRoot());
        var polisher = new IncidentPolisher(generator, promptProvider);
        var tooLongNote = new string('a', IncidentPolishingRules.MaxIncidentNoteLength + 1);

        var result = await polisher.PolishAsync(
            new PolishIncidentRequest(tooLongNote, IncidentAudience.Leadership),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_INCIDENT_NOTE", result.Error!.Code);
        await generator.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default!, default);
    }

    [Fact]
    public async Task PolishAsyncWhenRequestIsValidInvokesAiAndReturnsPolishedIncident()
    {
        var generator = Substitute.For<IAiTextGenerator>();
        generator.GenerateAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("{\"summary\":\"Database latency recovered\",\"impact\":\"Users saw degraded response times\",\"actionsTaken\":[\"Restarted the affected pod\"],\"currentStatus\":\"Service is stable again\",\"nextSteps\":[\"Continue monitoring database performance\"]}");

        var promptProvider = new FilePromptProvider(CreatePromptRoot());
        var polisher = new IncidentPolisher(generator, promptProvider);

        var result = await polisher.PolishAsync(
            new PolishIncidentRequest("db slow, restarted pod, looks OK now", IncidentAudience.Leadership),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Incident);
        Assert.Equal("Database latency recovered", result.Incident!.Summary);
        Assert.Equal("Leadership", result.Prompt!.AudienceName);
        await generator.Received(1).GenerateAsync(
            Arg.Is<string>(prompt => prompt.Contains("DevOps incident communication assistant", StringComparison.OrdinalIgnoreCase)),
            Arg.Is<string>(prompt => prompt.Contains("db slow, restarted pod, looks OK now", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PolishAsyncWhenAiOutputIsMalformedReturnsStructuredError()
    {
        var generator = Substitute.For<IAiTextGenerator>();
        generator.GenerateAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("{\"summary\":\"\",\"impact\":\"Users saw degraded response times\",\"actionsTaken\":[],\"currentStatus\":\"Service is stable again\",\"nextSteps\":[\"Continue monitoring\"]}");

        var promptProvider = new FilePromptProvider(CreatePromptRoot());
        var polisher = new IncidentPolisher(generator, promptProvider);

        var result = await polisher.PolishAsync(
            new PolishIncidentRequest("db slow, restarted pod, looks OK now", IncidentAudience.Customer),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_AI_OUTPUT", result.Error!.Code);
    }

    [Fact]
    public async Task PolishAsyncWhenAiOutputContainsEmptyArraysReturnsSuccess()
    {
        var generator = Substitute.For<IAiTextGenerator>();
        generator.GenerateAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns("{\"summary\":\"The incident is under control\",\"impact\":\"No user impact was observed\",\"actionsTaken\":[],\"currentStatus\":\"Monitoring continues\",\"nextSteps\":[]}");

        var promptProvider = new FilePromptProvider(CreatePromptRoot());
        var polisher = new IncidentPolisher(generator, promptProvider);

        var result = await polisher.PolishAsync(
            new PolishIncidentRequest("minor slowdown resolved", IncidentAudience.Customer),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Incident!.ActionsTaken);
        Assert.Empty(result.Incident.NextSteps);
    }

    private static string CreatePromptRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"devops-ai-prompt-tests-{Guid.NewGuid():N}");
        var folder = Path.Combine(root, "incident-polisher");
        Directory.CreateDirectory(folder);

        File.WriteAllText(Path.Combine(folder, "system.md"), "You are a DevOps incident communication assistant. Transform rough technical incident notes into clear, professional updates.");
        File.WriteAllText(Path.Combine(folder, "leadership.md"), "Write an executive briefing focused on business impact, current operational status, actions taken, and recommended next steps. Keep the tone concise, factual, and decision-oriented.");
        File.WriteAllText(Path.Combine(folder, "customer.md"), "Write a customer-facing update using plain language, reassurance, and clear status. Avoid internal jargon, unnecessary technical detail, blame, and unsupported root-cause claims.");

        return folder;
    }
}
