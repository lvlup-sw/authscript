// =============================================================================
// <copyright file="SeedDemoDataTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data;

using Gateway.API.Data;

/// <summary>
/// Tests for <see cref="SeedDemoData"/> static seed generator.
/// </summary>
public class SeedDemoDataTests
{
    [Test]
    public async Task SeedDemoData_GeneratesCorrectCount_48Requests()
    {
        var requests = SeedDemoData.GenerateSeedRequests();

        await Assert.That(requests.Count).IsEqualTo(48);
    }

    [Test]
    public async Task SeedDemoData_DistributesStatuses_PerSpec()
    {
        var requests = SeedDemoData.GenerateSeedRequests();

        var statusCounts = requests
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        await Assert.That(statusCounts["processing"]).IsEqualTo(6);
        await Assert.That(statusCounts["ready"]).IsEqualTo(8);
        await Assert.That(statusCounts["submitted"]).IsEqualTo(15);
        await Assert.That(statusCounts["waiting_for_insurance"]).IsEqualTo(8);
        await Assert.That(statusCounts["approved"]).IsEqualTo(9);
        await Assert.That(statusCounts["denied"]).IsEqualTo(2);
    }

    [Test]
    public async Task SeedDemoData_UsesAllTestPatients()
    {
        var requests = SeedDemoData.GenerateSeedRequests();

        var patientIds = requests.Select(r => r.PatientId).Distinct().ToList();

        await Assert.That(patientIds).Contains("60178");
        await Assert.That(patientIds).Contains("60179");
        await Assert.That(patientIds).Contains("60180");
        await Assert.That(patientIds).Contains("60181");
        await Assert.That(patientIds).Contains("60182");
        await Assert.That(patientIds).Contains("60183");
        await Assert.That(patientIds).Contains("60184");
    }

    [Test]
    public async Task SeedDemoData_IncludesMultipleProcedures()
    {
        var requests = SeedDemoData.GenerateSeedRequests();

        var cptCodes = requests.Select(r => r.ProcedureCode).Distinct().ToList();

        await Assert.That(cptCodes).Contains("72148");
        await Assert.That(cptCodes).Contains("72141");
        await Assert.That(cptCodes).Contains("74177");
    }

    [Test]
    public async Task SeedDemoData_IncludesMultiplePayers()
    {
        var requests = SeedDemoData.GenerateSeedRequests();

        var payers = requests.Select(r => r.Payer).Distinct().ToList();

        await Assert.That(payers).Contains("Aetna");
        await Assert.That(payers).Contains("United Healthcare");
        await Assert.That(payers).Contains("Cigna");
    }

    [Test]
    public async Task SeedDemoData_ConfidenceScoresInRange()
    {
        var requests = SeedDemoData.GenerateSeedRequests();

        foreach (var request in requests)
        {
            await Assert.That(request.Confidence).IsGreaterThanOrEqualTo(60);
            await Assert.That(request.Confidence).IsLessThanOrEqualTo(98);
        }
    }

    [Test]
    public async Task SeedDemoData_TimestampsSpreadOverWeek()
    {
        var requests = SeedDemoData.GenerateSeedRequests();

        var timestamps = requests
            .Select(r => DateTimeOffset.Parse(r.CreatedAt))
            .ToList();

        var earliest = timestamps.Min();
        var latest = timestamps.Max();
        var span = latest - earliest;

        await Assert.That(span.TotalDays).IsGreaterThanOrEqualTo(6);
    }
}
