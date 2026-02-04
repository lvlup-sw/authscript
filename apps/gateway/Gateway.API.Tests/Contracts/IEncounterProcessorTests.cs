using Gateway.API.Contracts;

namespace Gateway.API.Tests.Contracts;

/// <summary>
/// Tests for the IEncounterProcessor interface contract.
/// </summary>
public class IEncounterProcessorTests
{
    [Test]
    public async Task IEncounterProcessor_Interface_Exists_WithProcessEncounterAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(IEncounterProcessor);

        // Act
        var method = interfaceType.GetMethod("ProcessEncounterAsync");

        // Assert
        await Assert.That(interfaceType.IsInterface).IsTrue();
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task));

        var parameters = method.GetParameters();
        await Assert.That(parameters.Length).IsEqualTo(3);
        await Assert.That(parameters[0].Name).IsEqualTo("encounterId");
        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(parameters[1].Name).IsEqualTo("patientId");
        await Assert.That(parameters[1].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(parameters[2].Name).IsEqualTo("ct");
        await Assert.That(parameters[2].ParameterType).IsEqualTo(typeof(CancellationToken));
    }
}
