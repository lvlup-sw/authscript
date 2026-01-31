using Gateway.API.Contracts.Http;
using Gateway.API.Services.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.API.Tests;

/// <summary>
/// Tests for DependencyExtensions DI registration methods.
/// </summary>
public class DependencyExtensionsTests
{
    private static IConfiguration CreateConfiguration(
        string fhirBaseUrl = "https://fhir.example.com/",
        string clientId = "test-client",
        string? tokenEndpoint = "https://auth.example.com/token")
    {
        var config = new Dictionary<string, string?>
        {
            ["Epic:FhirBaseUrl"] = fhirBaseUrl,
            ["Epic:ClientId"] = clientId,
            ["Epic:TokenEndpoint"] = tokenEndpoint,
            ["ClinicalQuery:ObservationLookbackMonths"] = "12",
            ["ClinicalQuery:ProcedureLookbackMonths"] = "24",
            ["Document:PriorAuthLoincCode"] = "68552-9",
            ["Document:PriorAuthLoincDisplay"] = "Prior Authorization",
            ["Caching:Enabled"] = "false",
            ["Caching:Duration"] = "00:05:00"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
    }

    [Test]
    public async Task AddEpicFhirServices_RegistersTokenStrategies()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        // Act
        services.AddEpicFhirServices(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var cdsStrategy = provider.GetService<CdsHookTokenStrategy>();
        var jwtStrategy = provider.GetService<JwtBackendTokenStrategy>();

        await Assert.That(cdsStrategy).IsNotNull();
        await Assert.That(jwtStrategy).IsNotNull();
    }

    [Test]
    public async Task AddEpicFhirServices_RegistersTokenStrategyResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        // Act
        services.AddEpicFhirServices(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var resolver = provider.GetService<ITokenStrategyResolver>();
        await Assert.That(resolver).IsNotNull();
        await Assert.That(resolver).IsTypeOf<TokenStrategyResolver>();
    }

    [Test]
    public async Task AddEpicFhirServices_RegistersFhirHttpClientProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddHttpClient();

        // Act
        services.AddEpicFhirServices(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var clientProvider = provider.GetService<IFhirHttpClientProvider>();
        await Assert.That(clientProvider).IsNotNull();
        await Assert.That(clientProvider).IsTypeOf<FhirHttpClientProvider>();
    }

    [Test]
    public async Task AddEpicFhirServices_RegistersEpicFhirOptionsWithValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        // Act
        services.AddEpicFhirServices(configuration);
        var provider = services.BuildServiceProvider();

        // Assert - Options should be resolvable
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<Gateway.API.Configuration.EpicFhirOptions>>();
        await Assert.That(options).IsNotNull();
        await Assert.That(options!.Value).IsNotNull();
        await Assert.That(options.Value.ClientId).IsEqualTo("test-client");
        await Assert.That(options.Value.FhirBaseUrl).IsEqualTo("https://fhir.example.com/");
    }

    [Test]
    public async Task AddEpicFhirServices_RegistersHttpClientNamedEpicFhir()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        // Act
        services.AddEpicFhirServices(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<IHttpClientFactory>();
        await Assert.That(factory).IsNotNull();

        var client = factory!.CreateClient("EpicFhir");
        await Assert.That(client).IsNotNull();
        await Assert.That(client.BaseAddress?.ToString()).IsEqualTo("https://fhir.example.com/");
    }
}
