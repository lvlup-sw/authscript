namespace Gateway.API.Tests.Contracts;

using Gateway.API.Contracts;
using Microsoft.Extensions.Hosting;

public class IEncounterPollingServiceTests
{
    [Test]
    public async Task IEncounterPollingService_Interface_Exists()
    {
        // Arrange
        var interfaceType = typeof(IEncounterPollingService);

        // Act & Assert - verify interface exists and extends IHostedService
        await Assert.That(interfaceType.IsInterface).IsTrue();
        await Assert.That(typeof(IHostedService).IsAssignableFrom(interfaceType)).IsTrue();
    }
}
