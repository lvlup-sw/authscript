using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Tests.Contracts;

/// <summary>
/// Tests for the IFhirClient interface contract.
/// </summary>
public sealed class IFhirClientTests
{
    [Test]
    public async Task IFhirClient_HasSearchServiceRequestsAsyncMethod()
    {
        // Arrange - verify interface has the method via reflection
        var interfaceType = typeof(IFhirClient);

        // Act
        var method = interfaceType.GetMethod("SearchServiceRequestsAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<List<ServiceRequestInfo>>));

        var parameters = method.GetParameters();
        await Assert.That(parameters.Length).IsEqualTo(3);
        await Assert.That(parameters[0].Name).IsEqualTo("patientId");
        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(parameters[1].Name).IsEqualTo("encounterId");
        await Assert.That(parameters[1].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(parameters[2].Name).IsEqualTo("cancellationToken");
        await Assert.That(parameters[2].ParameterType).IsEqualTo(typeof(CancellationToken));
    }
}
