namespace Gateway.API.Tests.Configuration;

using Gateway.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class AthenaOptionsTests
{
    [Test]
    public async Task AthenaOptions_WithRequiredProperties_BindsCorrectly()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Athena:ClientId"] = "test-client-id",
                ["Athena:ClientSecret"] = "test-secret",
                ["Athena:FhirBaseUrl"] = "https://api.platform.athenahealth.com/fhir/r4",
                ["Athena:TokenEndpoint"] = "https://api.platform.athenahealth.com/oauth2/token",
                ["Athena:PollingIntervalSeconds"] = "10",
                ["Athena:PracticeId"] = "123456"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<AthenaOptions>(config.GetSection("Athena"));
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<AthenaOptions>>().Value;

        // Assert
        await Assert.That(options.ClientId).IsEqualTo("test-client-id");
        await Assert.That(options.ClientSecret).IsEqualTo("test-secret");
        await Assert.That(options.FhirBaseUrl).IsEqualTo("https://api.platform.athenahealth.com/fhir/r4");
        await Assert.That(options.TokenEndpoint).IsEqualTo("https://api.platform.athenahealth.com/oauth2/token");
        await Assert.That(options.PollingIntervalSeconds).IsEqualTo(10);
        await Assert.That(options.PracticeId).IsEqualTo("123456");
    }

    [Test]
    public async Task AthenaOptions_Validate_ReturnsFalseWhenClientIdMissing()
    {
        // Arrange
        var options = new AthenaOptions
        {
            ClientId = "",
            FhirBaseUrl = "https://api.platform.athenahealth.com/fhir/r4",
            TokenEndpoint = "https://api.platform.athenahealth.com/oauth2/token"
        };

        // Act
        var isValid = options.IsValid();

        // Assert
        await Assert.That(isValid).IsFalse();
    }

    [Test]
    public async Task AthenaOptions_Validate_ReturnsFalseWhenTokenEndpointMissing()
    {
        // Arrange
        var options = new AthenaOptions
        {
            ClientId = "test-client-id",
            FhirBaseUrl = "https://api.platform.athenahealth.com/fhir/r4",
            TokenEndpoint = ""
        };

        // Act
        var isValid = options.IsValid();

        // Assert
        await Assert.That(isValid).IsFalse();
    }

    [Test]
    public async Task AthenaOptions_Validate_ReturnsFalseWhenFhirBaseUrlMissing()
    {
        // Arrange
        var options = new AthenaOptions
        {
            ClientId = "test-client-id",
            FhirBaseUrl = "",
            TokenEndpoint = "https://api.platform.athenahealth.com/oauth2/token"
        };

        // Act
        var isValid = options.IsValid();

        // Assert
        await Assert.That(isValid).IsFalse();
    }

    [Test]
    public async Task AthenaOptions_Validate_ReturnsTrueWhenAllRequiredFieldsPresent()
    {
        // Arrange
        var options = new AthenaOptions
        {
            ClientId = "test-client-id",
            FhirBaseUrl = "https://api.platform.athenahealth.com/fhir/r4",
            TokenEndpoint = "https://api.platform.athenahealth.com/oauth2/token"
        };

        // Act
        var isValid = options.IsValid();

        // Assert
        await Assert.That(isValid).IsTrue();
    }

    [Test]
    public async Task AthenaOptions_SectionName_IsAthena()
    {
        await Assert.That(AthenaOptions.SectionName).IsEqualTo("Athena");
    }

    [Test]
    public async Task AthenaOptions_PollingIntervalSeconds_DefaultsTo5()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Athena:ClientId"] = "test-client-id",
                ["Athena:FhirBaseUrl"] = "https://api.platform.athenahealth.com/fhir/r4",
                ["Athena:TokenEndpoint"] = "https://api.platform.athenahealth.com/oauth2/token"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<AthenaOptions>(config.GetSection("Athena"));
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<AthenaOptions>>().Value;

        // Assert
        await Assert.That(options.PollingIntervalSeconds).IsEqualTo(5);
    }
}
