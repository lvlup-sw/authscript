namespace Gateway.API.Tests.Configuration;

using Gateway.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class EpicFhirOptionsTests
{
    [Test]
    public async Task EpicFhirOptions_Binding_LoadsFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Epic:FhirBaseUrl"] = "https://fhir.epic.com/api/FHIR/R4",
                ["Epic:ClientId"] = "test-client-id",
                ["Epic:ClientSecret"] = "test-secret",
                ["Epic:TokenEndpoint"] = "https://fhir.epic.com/oauth2/token"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<EpicFhirOptions>(config.GetSection("Epic"));
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<EpicFhirOptions>>().Value;

        // Assert
        await Assert.That(options.FhirBaseUrl).IsEqualTo("https://fhir.epic.com/api/FHIR/R4");
        await Assert.That(options.ClientId).IsEqualTo("test-client-id");
        await Assert.That(options.ClientSecret).IsEqualTo("test-secret");
        await Assert.That(options.TokenEndpoint).IsEqualTo("https://fhir.epic.com/oauth2/token");
    }

    [Test]
    public async Task EpicFhirOptions_SectionName_IsEpic()
    {
        await Assert.That(EpicFhirOptions.SectionName).IsEqualTo("Epic");
    }
}
