namespace Gateway.API.Tests.Models;

using Gateway.API.Models;

public class ServiceRequestInfoTests
{
    [Test]
    public async Task ServiceRequestInfo_RequiredProperties_InitializesCorrectly()
    {
        // Arrange
        var code = new CodeableConcept { Text = "MRI Lumbar" };

        // Act
        var serviceRequest = new ServiceRequestInfo
        {
            Id = "sr-123",
            Status = "active",
            Code = code
        };

        // Assert
        await Assert.That(serviceRequest.Id).IsEqualTo("sr-123");
        await Assert.That(serviceRequest.Status).IsEqualTo("active");
        await Assert.That(serviceRequest.Code).IsNotNull();
        await Assert.That(serviceRequest.EncounterId).IsNull();
        await Assert.That(serviceRequest.AuthoredOn).IsNull();
    }

    [Test]
    public async Task ServiceRequestInfo_OptionalProperties_CanBeSet()
    {
        // Arrange
        var code = new CodeableConcept { Text = "Physical Therapy" };
        var authoredOn = DateTimeOffset.UtcNow;

        // Act
        var serviceRequest = new ServiceRequestInfo
        {
            Id = "sr-456",
            Status = "completed",
            Code = code,
            EncounterId = "enc-789",
            AuthoredOn = authoredOn
        };

        // Assert
        await Assert.That(serviceRequest.EncounterId).IsEqualTo("enc-789");
        await Assert.That(serviceRequest.AuthoredOn).IsEqualTo(authoredOn);
    }
}
