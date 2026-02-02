// =============================================================================
// <copyright file="AthenaQueryBuilderTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Services;

using Gateway.API.Services;

/// <summary>
/// Tests for the AthenaQueryBuilder utility.
/// </summary>
public sealed class AthenaQueryBuilderTests
{
    [Test]
    public async Task BuildEncounterQuery_FormatsAhPracticeCorrectly()
    {
        // Arrange
        var patientId = "patient-123";
        var encounterId = "encounter-456";
        var practiceId = "195900";

        // Act
        var query = AthenaQueryBuilder.BuildEncounterQuery(patientId, encounterId, practiceId);

        // Assert - verify ah-practice format
        await Assert.That(query).Contains("ah-practice=Organization/a-1.Practice-195900");
    }

    [Test]
    public async Task BuildEncounterQuery_IncludesAllRequiredParameters()
    {
        // Arrange
        var patientId = "patient-123";
        var encounterId = "encounter-456";
        var practiceId = "195900";

        // Act
        var query = AthenaQueryBuilder.BuildEncounterQuery(patientId, encounterId, practiceId);

        // Assert
        await Assert.That(query).Contains("patient=patient-123");
        await Assert.That(query).Contains("_id=encounter-456");
        await Assert.That(query).Contains("ah-practice=Organization/a-1.Practice-195900");
    }

    [Test]
    public async Task BuildEncounterQuery_ReturnsCorrectFullFormat()
    {
        // Arrange
        var patientId = "p1";
        var encounterId = "e1";
        var practiceId = "100";

        // Act
        var query = AthenaQueryBuilder.BuildEncounterQuery(patientId, encounterId, practiceId);

        // Assert
        await Assert.That(query).IsEqualTo("patient=p1&_id=e1&ah-practice=Organization/a-1.Practice-100");
    }
}
