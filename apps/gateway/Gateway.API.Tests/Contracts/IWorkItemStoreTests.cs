namespace Gateway.API.Tests.Contracts;

using Gateway.API.Contracts;
using Gateway.API.Models;

public class IWorkItemStoreTests
{
    [Test]
    public async Task IWorkItemStore_HasRequiredMethods()
    {
        // Arrange - Get the interface type
        var interfaceType = typeof(IWorkItemStore);

        // Act & Assert - Verify interface exists and has required methods
        await Assert.That(interfaceType).IsNotNull();
        await Assert.That(interfaceType.IsInterface).IsTrue();

        // Verify CreateAsync method
        var createMethod = interfaceType.GetMethod("CreateAsync");
        await Assert.That(createMethod).IsNotNull();

        // Verify GetByIdAsync method
        var getByIdMethod = interfaceType.GetMethod("GetByIdAsync");
        await Assert.That(getByIdMethod).IsNotNull();

        // Verify UpdateStatusAsync method
        var updateMethod = interfaceType.GetMethod("UpdateStatusAsync");
        await Assert.That(updateMethod).IsNotNull();

        // Verify GetByEncounterAsync method
        var getByEncounterMethod = interfaceType.GetMethod("GetByEncounterAsync");
        await Assert.That(getByEncounterMethod).IsNotNull();
    }

    [Test]
    public async Task IWorkItemStore_HasGetAllAsyncMethod()
    {
        // Arrange
        var type = typeof(IWorkItemStore);

        // Act
        var method = type.GetMethod("GetAllAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<List<WorkItem>>));
    }
}
