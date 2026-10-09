using DevOpsAI.Application;
using DevOpsAI.Domain;
using NSubstitute;

namespace DevOpsAI.ApplicationTests.Incidents;

public class IncidentPolisherTests
{
    [Fact]
    public async Task PolishAsyncWhenIncidentNoteIsEmptyReturnsValidationError()
    {
        var generator = Substitute.For<IAiTextGenerator>();
        var polisher = new IncidentPolisher(generator);

        var result = await polisher.PolishAsync(
            new PolishIncidentRequest("   ", IncidentAudience.Leadership),
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

        var polisher = new IncidentPolisher(generator);

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

        var polisher = new IncidentPolisher(generator);

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

        var polisher = new IncidentPolisher(generator);

        var result = await polisher.PolishAsync(
            new PolishIncidentRequest("minor slowdown resolved", IncidentAudience.Customer),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Incident!.ActionsTaken);
        Assert.Empty(result.Incident.NextSteps);
    }
}
