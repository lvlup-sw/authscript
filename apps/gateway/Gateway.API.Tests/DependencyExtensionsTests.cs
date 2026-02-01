namespace Gateway.API.Tests;

using Gateway.API.Contracts;
using Gateway.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Tests for AddGatewayServices dependency injection extension method.
/// </summary>
public class DependencyExtensionsTests
{
    [Test]
    public async Task AddGatewayServices_ResolvesIWorkItemStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateTestConfiguration();

        // Act
        services.AddGatewayServices(config);
        var provider = services.BuildServiceProvider();
        var workItemStore = provider.GetService<IWorkItemStore>();

        // Assert
        await Assert.That(workItemStore).IsNotNull();
        await Assert.That(workItemStore).IsTypeOf<InMemoryWorkItemStore>();
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["ClinicalQuery:ObservationLookbackMonths"] = "12",
            ["ClinicalQuery:ProcedureLookbackMonths"] = "12",
            ["Document:MaxSizeMb"] = "10",
            ["Caching:Enabled"] = "false",
            ["Caching:Duration"] = "00:05:00",
            ["Caching:LocalCacheDuration"] = "00:01:00"
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }
}
