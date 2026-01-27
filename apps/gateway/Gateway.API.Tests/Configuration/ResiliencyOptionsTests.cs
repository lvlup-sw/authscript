namespace Gateway.API.Tests.Configuration;

using Gateway.API.Configuration;

public class ResiliencyOptionsTests
{
    [Test]
    public async Task ResiliencyOptions_Defaults_HaveReasonableValues()
    {
        // Arrange & Act
        var options = new ResiliencyOptions();

        // Assert
        await Assert.That(options.MaxRetryAttempts).IsEqualTo(3);
        await Assert.That(options.RetryDelaySeconds).IsEqualTo(1.0);
        await Assert.That(options.TimeoutSeconds).IsEqualTo(10);
        await Assert.That(options.CircuitBreakerThreshold).IsEqualTo(5);
        await Assert.That(options.CircuitBreakerDurationSeconds).IsEqualTo(30);
    }

    [Test]
    public async Task ResiliencyOptions_SectionName_IsResilience()
    {
        await Assert.That(ResiliencyOptions.SectionName).IsEqualTo("Resilience");
    }
}
