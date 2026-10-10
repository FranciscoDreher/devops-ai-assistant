using DevOpsAI.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsAI.IntegrationTests.AI;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAiServicesRegistersGenerator()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Model"] = "gpt-4o-mini",
                ["Ai:Endpoint"] = "https://example.test",
                ["Ai:ApiKey"] = "test-key"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddAiServices(configuration);

        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<IAiTextGenerator>();

        Assert.NotNull(generator);
    }
}
