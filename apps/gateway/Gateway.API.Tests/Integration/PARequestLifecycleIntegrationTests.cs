// =============================================================================
// <copyright file="PARequestLifecycleIntegrationTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Integration;

using System.Text.Json;
using Alba;

/// <summary>
/// Integration tests verifying the PA request lifecycle through GraphQL.
/// Creates, queries, and deletes PA requests through the full HTTP pipeline.
/// Tests run sequentially since they share an in-memory database.
/// </summary>
[Category("Integration")]
[NotInParallel]
[ClassDataSource<PARequestLifecycleAlbaBootstrap>(Shared = SharedType.PerTestSession)]
public sealed class PARequestLifecycleIntegrationTests
{
    private const string ApiKeyHeader = "X-API-Key";
    private const string GraphQLEndpoint = "/api/graphql";

    private readonly PARequestLifecycleAlbaBootstrap _fixture;

    public PARequestLifecycleIntegrationTests(PARequestLifecycleAlbaBootstrap fixture)
    {
        _fixture = fixture;
    }

    private void AddApiKey(Scenario s) => s.WithRequestHeader(ApiKeyHeader, PARequestLifecycleAlbaBootstrap.TestApiKey);

    private async Task<JsonElement> ExecuteGraphQL(string query, object? variables = null)
    {
        var body = new { query, variables };

        var result = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Post.Json(body).ToUrl(GraphQLEndpoint);
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var json = result.ReadAsText();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // If GraphQL returned errors, fail fast with the error messages
        if (root.TryGetProperty("errors", out var errors))
        {
            var errorText = errors.ToString();
            throw new InvalidOperationException($"GraphQL errors: {errorText}");
        }

        return root;
    }

    private object CreatePatientVariables(string idSuffix, string payer, string procedureCode, string diagnosisCode, string diagnosisName) =>
        new
        {
            input = new
            {
                patient = new
                {
                    id = $"test-{idSuffix}",
                    patientId = $"fhir-{idSuffix}",
                    name = $"Test Patient {idSuffix}",
                    mrn = $"MRN-{idSuffix}",
                    dob = "01/01/1990",
                    memberId = $"MEM-{idSuffix}",
                    payer,
                    address = "123 Test St",
                    phone = "(555) 000-0001",
                },
                procedureCode,
                diagnosisCode,
                diagnosisName,
            }
        };

    [Test]
    public async Task CreatePARequest_PersistsToDatabase_ReturnsInGetPARequests()
    {
        // Arrange
        var createMutation = """
            mutation CreatePA($input: CreatePARequestInput!) {
                createPARequest(input: $input) {
                    id
                    patientId
                    procedureCode
                    procedureName
                    diagnosis
                    diagnosisCode
                    status
                    payer
                    provider
                }
            }
            """;

        var variables = CreatePatientVariables("lifecycle-1", "Blue Cross Blue Shield", "72148", "M54.5", "Low Back Pain");

        // Act - create
        var createResult = await ExecuteGraphQL(createMutation, variables);
        var createData = createResult.GetProperty("data").GetProperty("createPARequest");
        var createdId = createData.GetProperty("id").GetString();

        // Assert - creation fields
        await Assert.That(createdId).IsNotNull();
        await Assert.That(createData.GetProperty("procedureCode").GetString()).IsEqualTo("72148");
        await Assert.That(createData.GetProperty("diagnosis").GetString()).IsEqualTo("Low Back Pain");
        await Assert.That(createData.GetProperty("status").GetString()).IsEqualTo("draft");

        // Act - query to verify persistence
        var getQuery = """
            query GetPA($id: String!) {
                paRequest(id: $id) {
                    id
                    patientId
                    procedureCode
                    diagnosis
                    status
                }
            }
            """;

        var getResult = await ExecuteGraphQL(getQuery, new { id = createdId });
        var getData = getResult.GetProperty("data").GetProperty("paRequest");

        // Assert - retrieved matches created
        await Assert.That(getData.GetProperty("id").GetString()).IsEqualTo(createdId);
        await Assert.That(getData.GetProperty("procedureCode").GetString()).IsEqualTo("72148");
        await Assert.That(getData.GetProperty("diagnosis").GetString()).IsEqualTo("Low Back Pain");
        await Assert.That(getData.GetProperty("status").GetString()).IsEqualTo("draft");
    }

    [Test]
    public async Task GetPAStats_ReflectsCurrentDatabaseState()
    {
        // Arrange - create a request so there's at least one
        var createMutation = """
            mutation CreatePA($input: CreatePARequestInput!) {
                createPARequest(input: $input) { id status }
            }
            """;

        var variables = CreatePatientVariables("stats-1", "Aetna", "70553", "G43.909", "Migraine, Unspecified");
        await ExecuteGraphQL(createMutation, variables);

        // Act - query stats
        var statsQuery = """
            {
                paStats {
                    total
                    ready
                    submitted
                    waitingForInsurance
                    attention
                }
            }
            """;

        var statsResult = await ExecuteGraphQL(statsQuery);
        var stats = statsResult.GetProperty("data").GetProperty("paStats");

        // Assert - total should be at least 1
        await Assert.That(stats.GetProperty("total").GetInt32()).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task DeletePARequest_RemovesFromDatabase()
    {
        // Arrange - create a request
        var createMutation = """
            mutation CreatePA($input: CreatePARequestInput!) {
                createPARequest(input: $input) { id }
            }
            """;

        var variables = CreatePatientVariables("delete-1", "Cigna", "27447", "M17.11", "Primary Osteoarthritis, Right Knee");

        var createResult = await ExecuteGraphQL(createMutation, variables);
        var createdId = createResult.GetProperty("data").GetProperty("createPARequest").GetProperty("id").GetString();
        await Assert.That(createdId).IsNotNull();

        // Act - delete
        var deleteMutation = """
            mutation DeletePA($id: String!) {
                deletePARequest(id: $id)
            }
            """;

        var deleteResult = await ExecuteGraphQL(deleteMutation, new { id = createdId });
        var deleted = deleteResult.GetProperty("data").GetProperty("deletePARequest").GetBoolean();
        await Assert.That(deleted).IsTrue();

        // Assert - verify gone
        var getQuery = """
            query GetPA($id: String!) {
                paRequest(id: $id) { id }
            }
            """;

        var getResult = await ExecuteGraphQL(getQuery, new { id = createdId });
        var getData = getResult.GetProperty("data").GetProperty("paRequest");
        await Assert.That(getData.ValueKind).IsEqualTo(JsonValueKind.Null);
    }
}
