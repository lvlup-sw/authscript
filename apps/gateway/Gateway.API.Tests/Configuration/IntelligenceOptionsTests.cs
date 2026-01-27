namespace Gateway.API.Tests.Configuration;

using Gateway.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class IntelligenceOptionsTests
{
    [Test]
    public async Task IntelligenceOptions_Binding_LoadsFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Intelligence:BaseUrl"] = "http://localhost:8000",
                ["Intelligence:TimeoutSeconds"] = "60"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<IntelligenceOptions>(config.GetSection("Intelligence"));
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<IntelligenceOptions>>().Value;

        // Assert
        await Assert.That(options.BaseUrl).IsEqualTo("http://localhost:8000");
        await Assert.That(options.TimeoutSeconds).IsEqualTo(60);
    }

    [Test]
    public async Task IntelligenceOptions_TimeoutSeconds_DefaultsTo30()
    {
        // Arrange & Act
        var options = new IntelligenceOptions { BaseUrl = "http://test" };

        // Assert
        await Assert.That(options.TimeoutSeconds).IsEqualTo(30);
    }

    [Test]
    public async Task IntelligenceOptions_SectionName_IsIntelligence()
    {
        await Assert.That(IntelligenceOptions.SectionName).IsEqualTo("Intelligence");
    }
}
