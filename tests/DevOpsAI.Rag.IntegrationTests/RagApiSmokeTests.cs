using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsAI.Rag.IntegrationTests;

public class RagApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RagApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpointShouldReturnHealthyAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("healthy", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultHostUrlBindsToLoopback()
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        Assert.Equal("http://127.0.0.1:5082", configuration["Urls"]);
    }
}
