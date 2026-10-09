using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevOpsAI.IntegrationTests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RootEndpointShouldReturnReadyStatusAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();

        Assert.Contains("DevOpsAI.Api", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ready", payload, StringComparison.OrdinalIgnoreCase);
    }
}
