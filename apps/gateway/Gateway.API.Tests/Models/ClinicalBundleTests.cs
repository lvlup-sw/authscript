using Gateway.API.Models;

namespace Gateway.API.Tests.Models;

public class ClinicalBundleTests
{
    [Test]
    public async Task ClinicalBundle_ServiceRequests_DefaultsToEmptyList()
    {
        // Arrange & Act
        var bundle = new ClinicalBundle
        {
            PatientId = "patient-123"
        };

        // Assert
        await Assert.That(bundle.ServiceRequests).IsNotNull();
        await Assert.That(bundle.ServiceRequests).IsEmpty();
    }

    [Test]
    public async Task ClinicalBundle_ServiceRequests_CanBeInitialized()
    {
        // Arrange
        var serviceRequest = new ServiceRequestInfo
        {
            Id = "sr-123",
            Status = "active",
            Code = new CodeableConcept { Text = "MRI" }
        };

        // Act
        var bundle = new ClinicalBundle
        {
            PatientId = "patient-123",
            ServiceRequests = [serviceRequest]
        };

        // Assert
        await Assert.That(bundle.ServiceRequests).HasCount().EqualTo(1);
        await Assert.That(bundle.ServiceRequests[0].Id).IsEqualTo("sr-123");
    }
}
