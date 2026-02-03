// =============================================================================
// <copyright file="PersistenceIntegrationTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Integration;

using Alba;
using Gateway.API.Models;

/// <summary>
/// Integration tests verifying persistence behavior through HTTP API.
/// These tests specifically validate that data persists across separate HTTP requests
/// using the EF Core in-memory database configured in GatewayAlbaBootstrap.
/// </summary>
[Category("Integration")]
[ClassDataSource<GatewayAlbaBootstrap>(Shared = SharedType.PerTestSession)]
public sealed class PersistenceIntegrationTests
{
    private const string ApiKeyHeader = "X-API-Key";
    private readonly GatewayAlbaBootstrap _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The Alba bootstrap fixture.</param>
    public PersistenceIntegrationTests(GatewayAlbaBootstrap fixture)
    {
        _fixture = fixture;
    }

    private void AddApiKey(Scenario s) => s.WithRequestHeader(ApiKeyHeader, GatewayAlbaBootstrap.TestApiKey);

    #region WorkItem Persistence Tests

    /// <summary>
    /// Verifies that work item data persists across separate HTTP requests.
    /// Creates a work item, then retrieves it in a new request to confirm persistence.
    /// </summary>
    [Test]
    public async Task WorkItem_CreateAndRetrieve_DataPersistsAcrossRequests()
    {
        // Arrange
        var patientId = $"persist-patient-{Guid.NewGuid():N}";
        var encounterId = $"persist-encounter-{Guid.NewGuid():N}";
        var request = new CreateWorkItemRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            ServiceRequestId = "sr-persist-test",
            ProcedureCode = "72148"
        };

        // Act - Create work item (first HTTP request)
        var createResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Post.Json(request).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(201);
        }).ConfigureAwait(false);

        var createdWorkItem = createResult.ReadAsJson<WorkItem>();
        await Assert.That(createdWorkItem).IsNotNull();

        var workItemId = createdWorkItem!.Id;

        // Act - Retrieve work item (separate HTTP request)
        var getResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Get.Url($"/api/work-items/{workItemId}");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var retrievedWorkItem = getResult.ReadAsJson<WorkItem>();

        // Assert - Data persisted correctly
        await Assert.That(retrievedWorkItem).IsNotNull();
        await Assert.That(retrievedWorkItem!.Id).IsEqualTo(workItemId);
        await Assert.That(retrievedWorkItem.PatientId).IsEqualTo(patientId);
        await Assert.That(retrievedWorkItem.EncounterId).IsEqualTo(encounterId);
        await Assert.That(retrievedWorkItem.ServiceRequestId).IsEqualTo("sr-persist-test");
        await Assert.That(retrievedWorkItem.ProcedureCode).IsEqualTo("72148");
    }

    /// <summary>
    /// Verifies that work item status updates persist across separate HTTP requests.
    /// Creates a work item, updates its status, then retrieves to confirm the change persisted.
    /// </summary>
    [Test]
    public async Task WorkItem_UpdateStatus_ChangePersists()
    {
        // Arrange - Create work item
        var patientId = $"status-patient-{Guid.NewGuid():N}";
        var encounterId = $"status-encounter-{Guid.NewGuid():N}";
        var createRequest = new CreateWorkItemRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            ServiceRequestId = "sr-status-test",
            ProcedureCode = "99213"
        };

        var createResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Post.Json(createRequest).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(201);
        }).ConfigureAwait(false);

        var createdWorkItem = createResult.ReadAsJson<WorkItem>();
        await Assert.That(createdWorkItem).IsNotNull();

        var workItemId = createdWorkItem!.Id;

        // Verify initial status
        await Assert.That(createdWorkItem.Status).IsEqualTo(WorkItemStatus.MissingData);

        // Act - Update status (second HTTP request)
        var updateRequest = new UpdateStatusRequest { Status = WorkItemStatus.ReadyForReview };
        var updateResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Put.Json(updateRequest).ToUrl($"/api/work-items/{workItemId}/status");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var updatedWorkItem = updateResult.ReadAsJson<WorkItem>();
        await Assert.That(updatedWorkItem).IsNotNull();
        await Assert.That(updatedWorkItem!.Status).IsEqualTo(WorkItemStatus.ReadyForReview);

        // Act - Retrieve work item (third HTTP request to verify persistence)
        var getResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Get.Url($"/api/work-items/{workItemId}");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var retrievedWorkItem = getResult.ReadAsJson<WorkItem>();

        // Assert - Status change persisted
        await Assert.That(retrievedWorkItem).IsNotNull();
        await Assert.That(retrievedWorkItem!.Id).IsEqualTo(workItemId);
        await Assert.That(retrievedWorkItem.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    #endregion

    #region PatientRegistration Persistence Tests

    /// <summary>
    /// Verifies that patient registration data persists across separate HTTP requests.
    /// Registers a patient, then retrieves the patient in a new request to confirm persistence.
    /// </summary>
    [Test]
    public async Task PatientRegistration_RegisterAndGet_DataPersists()
    {
        // Arrange
        var patientId = $"reg-patient-{Guid.NewGuid():N}";
        var encounterId = $"reg-encounter-{Guid.NewGuid():N}";
        var practiceId = "persist-practice-123";
        var registerRequest = new RegisterPatientRequest
        {
            PatientId = patientId,
            EncounterId = encounterId,
            PracticeId = practiceId
        };

        // Act - Register patient (first HTTP request)
        var registerResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Post.Json(registerRequest).ToUrl("/api/patients/register");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var registerResponse = registerResult.ReadAsJson<RegisterPatientResponse>();
        await Assert.That(registerResponse).IsNotNull();
        await Assert.That(registerResponse!.WorkItemId).IsNotNull();
        await Assert.That(registerResponse.WorkItemId).IsNotEmpty();

        // Act - Get patient (separate HTTP request)
        var getResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Get.Url($"/api/patients/{patientId}");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var retrievedPatient = getResult.ReadAsJson<RegisteredPatient>();

        // Assert - Data persisted correctly
        await Assert.That(retrievedPatient).IsNotNull();
        await Assert.That(retrievedPatient!.PatientId).IsEqualTo(patientId);
        await Assert.That(retrievedPatient.EncounterId).IsEqualTo(encounterId);
        await Assert.That(retrievedPatient.PracticeId).IsEqualTo(practiceId);
        await Assert.That(retrievedPatient.WorkItemId).IsEqualTo(registerResponse.WorkItemId);
    }

    /// <summary>
    /// Verifies that unregistering a patient removes data from the database.
    /// Registers a patient, unregisters them, then confirms the patient is no longer retrievable.
    /// </summary>
    [Test]
    public async Task PatientRegistration_Unregister_RemovesFromDatabase()
    {
        // Arrange - Register patient first
        var patientId = $"unreg-patient-{Guid.NewGuid():N}";
        var registerRequest = new RegisterPatientRequest
        {
            PatientId = patientId,
            EncounterId = "unreg-encounter-persist-test",
            PracticeId = "unreg-practice-123"
        };

        var registerResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Post.Json(registerRequest).ToUrl("/api/patients/register");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var registerResponse = registerResult.ReadAsJson<RegisterPatientResponse>();
        await Assert.That(registerResponse).IsNotNull();

        // Verify patient exists
        await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Get.Url($"/api/patients/{patientId}");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        // Act - Unregister patient (deletes from database)
        await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Delete.Url($"/api/patients/{patientId}");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        // Act - Try to get patient (should return 404)
        await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Get.Url($"/api/patients/{patientId}");
            s.StatusCodeShouldBe(404);
        }).ConfigureAwait(false);
    }

    #endregion
}
