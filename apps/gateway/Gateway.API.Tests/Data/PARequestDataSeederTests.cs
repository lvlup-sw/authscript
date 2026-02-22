// =============================================================================
// <copyright file="PARequestDataSeederTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data;

using Gateway.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Tests for <see cref="PARequestDataSeeder"/>.
/// </summary>
public class PARequestDataSeederTests
{
    private GatewayDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GatewayDbContext(options);
    }

    [Test]
    public async Task SeedAsync_EmptyDatabase_SeedsData()
    {
        // Arrange
        using var context = CreateDbContext();
        var logger = Substitute.For<ILogger<PARequestDataSeeder>>();
        var seeder = new PARequestDataSeeder(logger);

        // Act
        await seeder.SeedAsync(context, CancellationToken.None);

        // Assert
        var count = await context.PriorAuthRequests.CountAsync();
        await Assert.That(count).IsEqualTo(4);
    }

    [Test]
    public async Task SeedAsync_ExistingData_SkipsSeeding()
    {
        // Arrange
        using var context = CreateDbContext();
        var logger = Substitute.For<ILogger<PARequestDataSeeder>>();
        var seeder = new PARequestDataSeeder(logger);

        // Seed once
        await seeder.SeedAsync(context, CancellationToken.None);

        // Act — seed again
        await seeder.SeedAsync(context, CancellationToken.None);

        // Assert — still 4, not 8
        var count = await context.PriorAuthRequests.CountAsync();
        await Assert.That(count).IsEqualTo(4);
    }

    [Test]
    public async Task SeedAsync_CreatesVariousStatuses()
    {
        // Arrange
        using var context = CreateDbContext();
        var logger = Substitute.For<ILogger<PARequestDataSeeder>>();
        var seeder = new PARequestDataSeeder(logger);

        // Act
        await seeder.SeedAsync(context, CancellationToken.None);

        // Assert — should have draft, ready, and submitted statuses
        var statuses = await context.PriorAuthRequests
            .Select(e => e.Status)
            .Distinct()
            .ToListAsync();

        await Assert.That(statuses).Contains("draft");
        await Assert.That(statuses).Contains("ready");
        await Assert.That(statuses).Contains("submitted");
    }

    [Test]
    public async Task SeedAsync_SetsRequiredFields()
    {
        // Arrange
        using var context = CreateDbContext();
        var logger = Substitute.For<ILogger<PARequestDataSeeder>>();
        var seeder = new PARequestDataSeeder(logger);

        // Act
        await seeder.SeedAsync(context, CancellationToken.None);

        // Assert — all entities have required fields
        var entities = await context.PriorAuthRequests.ToListAsync();
        foreach (var entity in entities)
        {
            await Assert.That(entity.Id).IsNotNull().And.IsNotEqualTo(string.Empty);
            await Assert.That(entity.PatientId).IsNotNull().And.IsNotEqualTo(string.Empty);
            await Assert.That(entity.PatientName).IsNotNull().And.IsNotEqualTo(string.Empty);
            await Assert.That(entity.ProcedureCode).IsNotNull().And.IsNotEqualTo(string.Empty);
            await Assert.That(entity.ProcedureName).IsNotNull().And.IsNotEqualTo(string.Empty);
        }
    }

    [Test]
    public async Task CreateDemoEntities_ReturnsExpectedCount()
    {
        var entities = PARequestDataSeeder.CreateDemoEntities(DateTimeOffset.UtcNow);

        await Assert.That(entities.Length).IsEqualTo(4);
    }

    [Test]
    public async Task CreateDemoEntities_ReadyEntities_HaveCriteria()
    {
        var entities = PARequestDataSeeder.CreateDemoEntities(DateTimeOffset.UtcNow);
        var readyEntities = entities.Where(e => e.Status == "ready").ToList();

        foreach (var entity in readyEntities)
        {
            await Assert.That(entity.CriteriaJson).IsNotNull().And.IsNotEqualTo(string.Empty);
            await Assert.That(entity.Confidence).IsGreaterThan(0);
            await Assert.That(entity.ReadyAt).IsNotNull();
        }
    }
}
