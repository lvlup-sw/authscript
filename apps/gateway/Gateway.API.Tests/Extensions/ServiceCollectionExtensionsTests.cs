namespace Gateway.API.Tests.Extensions;

using Gateway.API.Configuration;
using Gateway.API.Contracts.Fhir;
using Gateway.API.Contracts.Http;
using Gateway.API.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Test]
    public async Task AddGatewayServices_RegistersHttpClientProvider()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Epic:FhirBaseUrl"] = "https://fhir.test/",
                ["Epic:ClientId"] = "test",
                ["Intelligence:BaseUrl"] = "http://localhost:8000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGatewayServices(config);

        var provider = services.BuildServiceProvider();

        // Act & Assert
        var httpClientProvider = provider.GetService<IHttpClientProvider>();
        await Assert.That(httpClientProvider).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_RegistersFhirSerializer()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Epic:FhirBaseUrl"] = "https://fhir.test/",
                ["Epic:ClientId"] = "test",
                ["Intelligence:BaseUrl"] = "http://localhost:8000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGatewayServices(config);

        var provider = services.BuildServiceProvider();

        // Act & Assert
        var fhirSerializer = provider.GetService<IFhirSerializer>();
        await Assert.That(fhirSerializer).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_RegistersNamedHttpClients()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Epic:FhirBaseUrl"] = "https://fhir.test/",
                ["Epic:ClientId"] = "test",
                ["Intelligence:BaseUrl"] = "http://localhost:8000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGatewayServices(config);

        var provider = services.BuildServiceProvider();

        // Act
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var epicClient = factory.CreateClient("EpicFhir");
        var intelligenceClient = factory.CreateClient("Intelligence");

        // Assert
        await Assert.That(epicClient.BaseAddress!.ToString()).IsEqualTo("https://fhir.test/");
        await Assert.That(intelligenceClient.BaseAddress!.ToString()).IsEqualTo("http://localhost:8000/");
    }
}
