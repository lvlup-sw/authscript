// =============================================================================
// <copyright file="DependencyExtensionsTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests;

using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Data;
using Gateway.API.Services;
using Gateway.API.Services.Decorators;
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
    public async Task AddIntelligenceClient_RegistersHttpClientForIntelligenceClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateTestConfiguration();
        services.AddSingleton<IConfiguration>(config);
        services.AddHybridCache();
        services.AddOptions<CachingSettings>()
            .BindConfiguration(CachingSettings.SectionName);

        // Act
        services.AddIntelligenceClient();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetService<IIntelligenceClient>();

        // Assert
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task AddIntelligenceClient_AppliesCachingDecorator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateTestConfiguration();
        services.AddSingleton<IConfiguration>(config);
        services.AddHybridCache();
        services.AddOptions<CachingSettings>()
            .BindConfiguration(CachingSettings.SectionName);

        // Act
        services.AddIntelligenceClient();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetService<IIntelligenceClient>();

        // Assert - Resolved service should be the caching decorator
        await Assert.That(client).IsTypeOf<CachingIntelligenceClient>();
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
            ["Caching:LocalCacheDuration"] = "00:01:00",
            ["Intelligence:BaseUrl"] = "http://localhost:8000",
            ["Intelligence:TimeoutSeconds"] = "30"
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }
}
