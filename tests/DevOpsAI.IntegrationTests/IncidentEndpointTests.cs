using System.Net;
using System.Net.Http.Json;
using DevOpsAI.Application;
using DevOpsAI.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevOpsAI.IntegrationTests;

public class IncidentEndpointTests : IClassFixture<IncidentEndpointWebApplicationFactory>
{
    private readonly IncidentEndpointWebApplicationFactory _factory;

    public IncidentEndpointTests(IncidentEndpointWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostPolishIncidentReturnsOkForValidRequestAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/incidents/polish", new
        {
            incidentNote = "Database latency spiked after deploy. Restarted pod and traffic recovered.",
            audience = "leadership"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IncidentApiResponse>();

        Assert.NotNull(payload);
        Assert.Equal("Database latency recovered", payload!.Summary);
    }

    [Fact]
    public async Task PostPolishIncidentReturnsBadRequestForInvalidRequestAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/incidents/polish", new
        {
            incidentNote = "   ",
            audience = "leadership"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("INVALID_INCIDENT_NOTE", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostPolishIncidentReturnsProblemDetailsWithCodeForUnhandledExceptionAsync()
    {
        using var client = new IncidentEndpointExceptionWebApplicationFactory().CreateClient();

        var response = await client.PostAsJsonAsync("/api/incidents/polish", new
        {
            incidentNote = "Database was slow during deployment.",
            audience = "leadership"
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("500", payload, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record IncidentApiResponse
    {
        public string Summary { get; init; } = string.Empty;

        public string Audience { get; init; } = string.Empty;
    }
}

public sealed class IncidentEndpointExceptionWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAiTextGenerator>();
            services.RemoveAll<IPromptProvider>();

            services.AddSingleton<IAiTextGenerator, ThrowingAiTextGenerator>();
            services.AddSingleton<IPromptProvider, FakePromptProvider>();
        });
    }

    private sealed class ThrowingAiTextGenerator : IAiTextGenerator
    {
        public Task<string> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated AI failure");
        }
    }

    private sealed class FakePromptProvider : IPromptProvider
    {
        public Task<string> GetSystemPromptAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult("You are a DevOps incident communication assistant.");
        }

        public Task<string> GetAudiencePromptAsync(IncidentAudience audience, CancellationToken cancellationToken)
        {
            return Task.FromResult(audience == IncidentAudience.Leadership
                ? "Write an executive briefing focused on business impact, current operational status, actions taken, and recommended next steps."
                : "Write a customer-facing update with reassurance, clear status, and plain language.");
        }
    }
}

public sealed class IncidentEndpointWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAiTextGenerator>();
            services.RemoveAll<IPromptProvider>();

            services.AddSingleton<IAiTextGenerator, FakeAiTextGenerator>();
            services.AddSingleton<IPromptProvider, FakePromptProvider>();
        });
    }

    private sealed class FakeAiTextGenerator : IAiTextGenerator
    {
        public Task<string> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken)
        {
            return Task.FromResult("{\"summary\":\"Database latency recovered\",\"impact\":\"Users experienced degraded performance during the incident\",\"actionsTaken\":[\"Restarted the affected pod\"],\"currentStatus\":\"Service performance is currently back to normal\",\"nextSteps\":[\"Continue monitoring database performance\"]}");
        }
    }

    private sealed class FakePromptProvider : IPromptProvider
    {
        public Task<string> GetSystemPromptAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult("You are a DevOps incident communication assistant.");
        }

        public Task<string> GetAudiencePromptAsync(IncidentAudience audience, CancellationToken cancellationToken)
        {
            return Task.FromResult(audience == IncidentAudience.Leadership
                ? "Write an executive briefing focused on business impact, current operational status, actions taken, and recommended next steps."
                : "Write a customer-facing update with reassurance, clear status, and plain language.");
        }
    }
}
