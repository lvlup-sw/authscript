// =============================================================================
// <copyright file="ProcessPARequestIntegrationTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using System.Text.Json;
using Alba;

namespace Gateway.API.Tests.Integration;

/// <summary>
/// Integration tests for the ProcessPARequest GraphQL mutation.
/// Exercises the full mutation through the ASP.NET pipeline with mocked
/// FHIR and Intelligence external dependencies.
/// </summary>
[Category("Integration")]
[ClassDataSource<ProcessPARequestAlbaBootstrap>(Shared = SharedType.PerTestSession)]
public sealed class ProcessPARequestIntegrationTests
{
    private readonly ProcessPARequestAlbaBootstrap _fixture;

    public ProcessPARequestIntegrationTests(ProcessPARequestAlbaBootstrap fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task ProcessPARequest_ViaGraphQL_ReturnsReadyStatusWithAnalysis()
    {
        // Arrange - Get a pre-seeded PA request ID (PA-001 uses patient 60178, procedure 72148)
        var listResult = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new
            {
                query = "{ paRequests { id status patientId procedureCode } }"
            }).ToUrl("/api/graphql");
            s.StatusCodeShouldBe(200);
        }).ConfigureAwait(false);

        var listBody = listResult.ReadAsText();
        using var listDoc = JsonDocument.Parse(listBody);
        var requests = listDoc.RootElement.GetProperty("data").GetProperty("paRequests");

        // Find PA-001 (Donna Sandbox, procedure 72148, status "ready")
        string paRequestId = "PA-001";
        var found = false;
        foreach (var req in requests.EnumerateArray())
        {
            if (req.GetProperty("id").GetString() == paRequestId)
            {
                found = true;
                break;
            }
        }

        await Assert.That(found).IsTrue();

        // Act - Process the PA request via GraphQL mutation
        var processResult = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new
            {
                query = @"mutation($id: String!) {
                    processPARequest(id: $id) {
                        id
                        status
                        confidence
                        clinicalSummary
                        criteria { met label reason }
                    }
                }",
                variables = new { id = paRequestId }
            }).ToUrl("/api/graphql");
            s.StatusCodeShouldBe(200);
        }).ConfigureAwait(false);

        // Assert
        var processBody = processResult.ReadAsText();
        using var processDoc = JsonDocument.Parse(processBody);
        var data = processDoc.RootElement.GetProperty("data").GetProperty("processPARequest");

        // ID should match
        var returnedId = data.GetProperty("id").GetString();
        await Assert.That(returnedId).IsEqualTo(paRequestId);

        // Status should be "ready" after processing
        var status = data.GetProperty("status").GetString();
        await Assert.That(status).IsEqualTo("ready");

        // Confidence should be 92 (0.92 * 100)
        var confidence = data.GetProperty("confidence").GetInt32();
        await Assert.That(confidence).IsEqualTo(92);

        // Clinical summary should come from the mock Intelligence response (not contain "STUB")
        var clinicalSummary = data.GetProperty("clinicalSummary").GetString();
        await Assert.That(clinicalSummary).IsNotNull();
        await Assert.That(clinicalSummary!).DoesNotContain("STUB");
        await Assert.That(clinicalSummary).Contains("chronic low back pain");

        // Criteria should have 3 items from the mock evidence
        var criteria = data.GetProperty("criteria");
        await Assert.That(criteria.GetArrayLength()).IsEqualTo(3);

        // Verify first criterion (conservative_therapy -> MET)
        var firstCriterion = criteria[0];
        await Assert.That(firstCriterion.GetProperty("met").GetBoolean()).IsTrue();
        await Assert.That(firstCriterion.GetProperty("label").GetString()).IsEqualTo("conservative_therapy");
        await Assert.That(firstCriterion.GetProperty("reason").GetString()).Contains("physical therapy");
    }

    [Test]
    public async Task ProcessPARequest_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new
            {
                query = @"mutation($id: String!) {
                    processPARequest(id: $id) {
                        id
                        status
                    }
                }",
                variables = new { id = "NONEXISTENT-ID" }
            }).ToUrl("/api/graphql");
            s.StatusCodeShouldBe(200);
        }).ConfigureAwait(false);

        // Assert - processPARequest should return null for non-existent ID
        var body = result.ReadAsText();
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data").GetProperty("processPARequest");
        await Assert.That(data.ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task ProcessPARequest_CriteriaMapping_AllMetStatusesMappedCorrectly()
    {
        // Arrange - Use PA-002 (different patient, still pre-seeded)
        var paRequestId = "PA-002";

        // Act
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new
            {
                query = @"mutation($id: String!) {
                    processPARequest(id: $id) {
                        id
                        criteria { met label reason }
                    }
                }",
                variables = new { id = paRequestId }
            }).ToUrl("/api/graphql");
            s.StatusCodeShouldBe(200);
        }).ConfigureAwait(false);

        // Assert - All 3 evidence items have status "MET" -> met: true
        var body = result.ReadAsText();
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data").GetProperty("processPARequest");
        var criteria = data.GetProperty("criteria");

        await Assert.That(criteria.GetArrayLength()).IsEqualTo(3);

        foreach (var criterion in criteria.EnumerateArray())
        {
            await Assert.That(criterion.GetProperty("met").GetBoolean()).IsTrue();
        }
    }
}
