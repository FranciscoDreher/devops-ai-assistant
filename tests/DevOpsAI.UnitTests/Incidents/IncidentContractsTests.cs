using DevOpsAI.Domain;

namespace DevOpsAI.UnitTests.Incidents;

public class IncidentContractsTests
{
    [Fact]
    public void IncidentAudienceShouldIncludeLeadershipAndCustomer()
    {
        Assert.Equal(nameof(IncidentAudience.Leadership), Enum.GetName(IncidentAudience.Leadership));
        Assert.Equal(nameof(IncidentAudience.Customer), Enum.GetName(IncidentAudience.Customer));
    }

    [Fact]
    public void PolishedIncidentShouldCaptureRequiredFields()
    {
        var incident = new PolishedIncident(
            summary: "A database issue impacted service latency.",
            impact: "Users experienced slower response times for 15 minutes.",
            actionsTaken: ["Restarted the affected pod", "Monitored database performance"],
            currentStatus: "Service performance has returned to normal.",
            nextSteps: ["Continue monitoring database latency"]);

        Assert.Equal("A database issue impacted service latency.", incident.Summary);
        Assert.Equal("Users experienced slower response times for 15 minutes.", incident.Impact);
        Assert.Equal(["Restarted the affected pod", "Monitored database performance"], incident.ActionsTaken);
        Assert.Equal("Service performance has returned to normal.", incident.CurrentStatus);
        Assert.Equal(["Continue monitoring database latency"], incident.NextSteps);
    }

    [Fact]
    public void PolishedIncidentShouldRejectEmptyCollections()
    {
        var exception = Assert.Throws<ArgumentException>(() => new PolishedIncident(
            summary: "Summary",
            impact: "Impact",
            actionsTaken: [],
            currentStatus: "Status",
            nextSteps: ["Next step"]));

        Assert.Contains("actionsTaken", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
