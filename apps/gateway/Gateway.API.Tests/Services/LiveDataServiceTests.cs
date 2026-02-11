// =============================================================================
// <copyright file="LiveDataServiceTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.Services;

namespace Gateway.API.Tests.Services;

public class LiveDataServiceTests
{
    [Test]
    public async Task LiveDataService_ImplementsIDataService()
    {
        // Arrange & Act
        var implements = typeof(LiveDataService).GetInterfaces()
            .Any(i => i.Name == "IDataService");

        // Assert
        await Assert.That(implements).IsTrue();
    }

    [Test]
    public async Task LiveDataService_GetPARequests_ThrowsNotImplemented()
    {
        // Arrange
        var service = new LiveDataService();

        // Act & Assert
        await Assert.That(() => service.GetPARequests()).Throws<NotImplementedException>();
    }

    [Test]
    public async Task LiveDataService_GetPARequest_ThrowsNotImplemented()
    {
        // Arrange
        var service = new LiveDataService();

        // Act & Assert
        await Assert.That(() => service.GetPARequest("PA-001")).Throws<NotImplementedException>();
    }

    [Test]
    public async Task LiveDataService_GetPAStats_ThrowsNotImplemented()
    {
        // Arrange
        var service = new LiveDataService();

        // Act & Assert
        await Assert.That(() => service.GetPAStats()).Throws<NotImplementedException>();
    }

    [Test]
    public async Task LiveDataService_Procedures_ThrowsNotImplemented()
    {
        // Arrange
        var service = new LiveDataService();

        // Act & Assert
        await Assert.That(() => service.Procedures).Throws<NotImplementedException>();
    }

    [Test]
    public async Task LiveDataService_ApprovePA_ThrowsNotImplemented()
    {
        // Arrange
        var service = new LiveDataService();

        // Act & Assert
        await Assert.That(() => service.ApprovePA("PA-001")).Throws<NotImplementedException>();
    }

    [Test]
    public async Task LiveDataService_DenyPA_ThrowsNotImplemented()
    {
        // Arrange
        var service = new LiveDataService();

        // Act & Assert
        await Assert.That(() => service.DenyPA("PA-001", "reason")).Throws<NotImplementedException>();
    }
}
