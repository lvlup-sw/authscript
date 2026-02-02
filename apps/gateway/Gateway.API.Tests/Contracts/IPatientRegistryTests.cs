using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Tests.Contracts;

/// <summary>
/// Tests for the IPatientRegistry interface contract.
/// </summary>
public class IPatientRegistryTests
{
    [Test]
    public async Task IPatientRegistry_InterfaceExists_WithExpectedMethods()
    {
        // Arrange
        var interfaceType = typeof(IPatientRegistry);

        // Assert interface exists
        await Assert.That(interfaceType).IsNotNull();
        await Assert.That(interfaceType.IsInterface).IsTrue();

        // Act - Get all methods
        var methods = interfaceType.GetMethods();
        var methodNames = methods.Select(m => m.Name).ToArray();

        // Assert expected methods exist
        await Assert.That(methodNames).Contains("RegisterAsync");
        await Assert.That(methodNames).Contains("GetActiveAsync");
        await Assert.That(methodNames).Contains("UnregisterAsync");
        await Assert.That(methodNames).Contains("GetAsync");
        await Assert.That(methodNames).Contains("UpdateAsync");
    }

    [Test]
    public async Task RegisterAsync_HasCorrectSignature()
    {
        // Arrange
        var interfaceType = typeof(IPatientRegistry);
        var method = interfaceType.GetMethod("RegisterAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task));

        var parameters = method.GetParameters();
        await Assert.That(parameters.Length).IsEqualTo(2);
        await Assert.That(parameters[0].Name).IsEqualTo("patient");
        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(RegisteredPatient));
        await Assert.That(parameters[1].Name).IsEqualTo("ct");
        await Assert.That(parameters[1].ParameterType).IsEqualTo(typeof(CancellationToken));
    }

    [Test]
    public async Task GetActiveAsync_HasCorrectSignature()
    {
        // Arrange
        var interfaceType = typeof(IPatientRegistry);
        var method = interfaceType.GetMethod("GetActiveAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<IReadOnlyList<RegisteredPatient>>));

        var parameters = method.GetParameters();
        await Assert.That(parameters.Length).IsEqualTo(1);
        await Assert.That(parameters[0].Name).IsEqualTo("ct");
        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(CancellationToken));
    }

    [Test]
    public async Task UnregisterAsync_HasCorrectSignature()
    {
        // Arrange
        var interfaceType = typeof(IPatientRegistry);
        var method = interfaceType.GetMethod("UnregisterAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task));

        var parameters = method.GetParameters();
        await Assert.That(parameters.Length).IsEqualTo(2);
        await Assert.That(parameters[0].Name).IsEqualTo("patientId");
        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(parameters[1].Name).IsEqualTo("ct");
        await Assert.That(parameters[1].ParameterType).IsEqualTo(typeof(CancellationToken));
    }

    [Test]
    public async Task GetAsync_HasCorrectSignature()
    {
        // Arrange
        var interfaceType = typeof(IPatientRegistry);
        var method = interfaceType.GetMethod("GetAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<RegisteredPatient?>));

        var parameters = method.GetParameters();
        await Assert.That(parameters.Length).IsEqualTo(2);
        await Assert.That(parameters[0].Name).IsEqualTo("patientId");
        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(parameters[1].Name).IsEqualTo("ct");
        await Assert.That(parameters[1].ParameterType).IsEqualTo(typeof(CancellationToken));
    }

    [Test]
    public async Task UpdateAsync_HasCorrectSignature()
    {
        // Arrange
        var interfaceType = typeof(IPatientRegistry);
        var method = interfaceType.GetMethod("UpdateAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<bool>));

        var parameters = method.GetParameters();
        await Assert.That(parameters.Length).IsEqualTo(4);
        await Assert.That(parameters[0].Name).IsEqualTo("patientId");
        await Assert.That(parameters[0].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(parameters[1].Name).IsEqualTo("lastPolled");
        await Assert.That(parameters[1].ParameterType).IsEqualTo(typeof(DateTimeOffset));
        await Assert.That(parameters[2].Name).IsEqualTo("status");
        await Assert.That(parameters[2].ParameterType).IsEqualTo(typeof(string));
        await Assert.That(parameters[3].Name).IsEqualTo("ct");
        await Assert.That(parameters[3].ParameterType).IsEqualTo(typeof(CancellationToken));
    }
}
