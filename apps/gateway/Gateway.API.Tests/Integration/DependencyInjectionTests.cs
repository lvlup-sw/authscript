namespace Gateway.API.Tests.Integration;

using Gateway.API.Contracts;
using Gateway.API.Contracts.Fhir;
using Gateway.API.Contracts.Http;
using Gateway.API.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NSubstitute;

/// <summary>
/// Integration tests for verifying DI container configuration.
/// </summary>
public class DependencyInjectionTests
{
    [Test]
    public async Task AddGatewayServices_CanResolveIFhirSerializer()
    {
        var provider = CreateServiceProvider();

        var serializer = provider.GetService<IFhirSerializer>();
        await Assert.That(serializer).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_CanResolveIHttpClientProvider()
    {
        var provider = CreateServiceProvider();

        var httpProvider = provider.GetService<IHttpClientProvider>();
        await Assert.That(httpProvider).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_CanResolveIEpicFhirClient()
    {
        var provider = CreateServiceProvider();

        var client = provider.GetService<IEpicFhirClient>();
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_CanResolveIFhirDataAggregator()
    {
        var provider = CreateServiceProvider();

        var aggregator = provider.GetService<IFhirDataAggregator>();
        await Assert.That(aggregator).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_CanResolveIIntelligenceClient()
    {
        var provider = CreateServiceProvider();

        var client = provider.GetService<IIntelligenceClient>();
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_CanResolveIEpicUploader()
    {
        var provider = CreateServiceProvider();

        var uploader = provider.GetService<IEpicUploader>();
        await Assert.That(uploader).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_CanResolveIPdfFormStamper()
    {
        var provider = CreateServiceProvider();

        var stamper = provider.GetService<IPdfFormStamper>();
        await Assert.That(stamper).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_CanResolveIDemoCacheService()
    {
        var provider = CreateServiceProvider();

        var cacheService = provider.GetService<IDemoCacheService>();
        await Assert.That(cacheService).IsNotNull();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var config = CreateTestConfiguration();
        var services = new ServiceCollection();

        // Register configuration as a service (required by DemoCacheService)
        services.AddSingleton<IConfiguration>(config);

        // Register mock IWebHostEnvironment (required by PdfFormStamper)
        var mockEnvironment = Substitute.For<IWebHostEnvironment>();
        mockEnvironment.ContentRootPath.Returns("/tmp");
        mockEnvironment.ContentRootFileProvider.Returns(Substitute.For<IFileProvider>());
        services.AddSingleton(mockEnvironment);

        services.AddLogging();
        services.AddGatewayServices(config);

        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Epic:FhirBaseUrl"] = "https://fhir.test/",
                ["Epic:ClientId"] = "test-client",
                ["Intelligence:BaseUrl"] = "http://localhost:8000"
            })
            .Build();
    }
}
