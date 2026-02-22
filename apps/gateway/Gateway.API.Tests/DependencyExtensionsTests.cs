// =============================================================================
// <copyright file="DependencyExtensionsTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests;

using Gateway.API.Contracts;
using Gateway.API.Data;
using Gateway.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Tests for AddGatewayServices dependency injection extension method.
/// </summary>
public class DependencyExtensionsTests
{
    [Test]
    public async Task AddGatewayServices_DoesNotRegisterWorkItemStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateTestConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGatewayServices();
        var provider = services.BuildServiceProvider();
        var workItemStore = provider.GetService<IWorkItemStore>();

        // Assert - AddGatewayServices does not register IWorkItemStore (use AddGatewayPersistence)
        await Assert.That(workItemStore).IsNull();
    }

    [Test]
    public async Task AddGatewayServices_DoesNotRegisterPatientRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateTestConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGatewayServices();
        var provider = services.BuildServiceProvider();

        // Assert - AddGatewayServices does not register IPatientRegistry (use AddGatewayPersistence)
        var registry = provider.GetService<IPatientRegistry>();
        await Assert.That(registry).IsNull();
    }

    [Test]
    public async Task AddGatewayPersistence_RegistersPostgresStores()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add required EF Core in-memory provider for testing
        services.AddDbContext<GatewayDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddGatewayPersistence();
        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var workItemStore = scope.ServiceProvider.GetService<IWorkItemStore>();
        var patientRegistry = scope.ServiceProvider.GetService<IPatientRegistry>();

        // Assert
        await Assert.That(workItemStore).IsNotNull();
        await Assert.That(workItemStore).IsTypeOf<PostgresWorkItemStore>();
        await Assert.That(patientRegistry).IsNotNull();
        await Assert.That(patientRegistry).IsTypeOf<PostgresPatientRegistry>();
    }

    [Test]
    public async Task AddGatewayPersistence_RegistersIPARequestStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<GatewayDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddGatewayPersistence();
        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetService<IPARequestStore>();

        // Assert
        await Assert.That(store).IsNotNull();
        await Assert.That(store).IsTypeOf<PostgresPARequestStore>();
    }

    [Test]
    public async Task AddGatewayServices_RegistersReferenceDataService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateTestConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGatewayServices();
        var provider = services.BuildServiceProvider();
        var refData = provider.GetService<ReferenceDataService>();

        // Assert
        await Assert.That(refData).IsNotNull();
    }

    [Test]
    public async Task AddGatewayServices_DoesNotRegisterMockDataService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateTestConfiguration();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddGatewayServices();

        // Assert - no registration with "Mock" in the implementation type name
        var mockRegistrations = services.Where(d =>
            d.ImplementationType?.Name.Contains("Mock", StringComparison.OrdinalIgnoreCase) == true);
        await Assert.That(mockRegistrations.Count()).IsEqualTo(0);
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["ClinicalQuery:ObservationLookbackMonths"] = "12",
            ["ClinicalQuery:ProcedureLookbackMonths"] = "12",
            ["Document:MaxSizeMb"] = "10",
            ["Caching:Enabled"] = "false",
            ["Caching:Duration"] = "00:05:00",
            ["Caching:LocalCacheDuration"] = "00:01:00"
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }
}
