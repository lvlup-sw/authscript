namespace Gateway.API.Tests;

using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Contracts.Http;
using Gateway.API.Models;
using Gateway.API.Services;
using Gateway.API.Services.Http;
using Gateway.API.Services.Polling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

/// <summary>
/// Tests for AddAthenaServices dependency injection extension method.
/// </summary>
public class DependencyExtensionsAthenaTests
{
    [Test]
    public async Task AddAthenaServices_RegistersAthenaOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Athena:ClientId"] = "test-client",
                ["Athena:FhirBaseUrl"] = "https://api.athena.test/fhir/r4",
                ["Athena:TokenEndpoint"] = "https://api.athena.test/oauth2/token"
            })
            .Build();

        // Act
        services.AddAthenaServices(config);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AthenaOptions>>();

        // Assert
        await Assert.That(options.Value.ClientId).IsEqualTo("test-client");
    }

    [Test]
    public async Task AddAthenaServices_RegistersAthenaTokenStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        var config = BuildValidConfig();

        // Act
        services.AddAthenaServices(config);
        var provider = services.BuildServiceProvider();
        var strategy = provider.GetService<ITokenAcquisitionStrategy>();

        // Assert
        await Assert.That(strategy).IsNotNull();
        await Assert.That(strategy).IsTypeOf<AthenaTokenStrategy>();
    }

    [Test]
    public async Task AddAthenaServices_RegistersPollingServiceAsHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        // Add mock IFhirHttpClient
        var mockFhirClient = Substitute.For<IFhirHttpClient>();
        services.AddSingleton(mockFhirClient);
        var config = BuildValidConfig();

        // Act
        services.AddAthenaServices(config);
        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();

        // Assert
        await Assert.That(hostedServices.Any(s => s is AthenaPollingService)).IsTrue();
    }

    [Test]
    public async Task AddAthenaServices_RegistersEncounterProcessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        // Add required dependencies
        services.AddSingleton(Substitute.For<IFhirDataAggregator>());
        services.AddSingleton(Substitute.For<IIntelligenceClient>());
        services.AddSingleton(Substitute.For<IPdfFormStamper>());
        services.AddSingleton(Substitute.For<IAnalysisResultStore>());
        services.AddSingleton(Substitute.For<INotificationHub>());
        var config = BuildValidConfig();

        // Act
        services.AddAthenaServices(config);
        var provider = services.BuildServiceProvider();
        var processor = provider.GetService<IEncounterProcessor>();

        // Assert
        await Assert.That(processor).IsNotNull();
    }

    [Test]
    public async Task AddAthenaServices_ConfiguresAthenaHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildValidConfig();

        // Act
        services.AddAthenaServices(config);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("Athena");

        // Assert
        await Assert.That(client).IsNotNull();
    }

    private static IConfiguration BuildValidConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Athena:ClientId"] = "test-client",
                ["Athena:ClientSecret"] = "test-secret",
                ["Athena:FhirBaseUrl"] = "https://api.athena.test/fhir/r4",
                ["Athena:TokenEndpoint"] = "https://api.athena.test/oauth2/token",
                ["Athena:PollingIntervalSeconds"] = "5"
            })
            .Build();
    }
}
