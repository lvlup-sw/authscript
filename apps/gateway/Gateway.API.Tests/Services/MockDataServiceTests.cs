// =============================================================================
// <copyright file="MockDataServiceTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.Services;

namespace Gateway.API.Tests.Services;

public class MockDataServiceTests
{
    [Test]
    public async Task MockDataService_ImplementsIDataService()
    {
        // Arrange & Act
        var implements = typeof(MockDataService).GetInterfaces()
            .Any(i => i.Name == "IDataService");

        // Assert
        await Assert.That(implements).IsTrue();
    }

    [Test]
    public async Task IDataService_CanBeAssignedFromMockDataService()
    {
        // Arrange
        var mock = new MockDataService();

        // Act
        IDataService service = mock;
        var requests = service.GetPARequests();

        // Assert
        await Assert.That(requests).IsNotNull();
        await Assert.That(requests.Count).IsGreaterThanOrEqualTo(0);
    }
}
