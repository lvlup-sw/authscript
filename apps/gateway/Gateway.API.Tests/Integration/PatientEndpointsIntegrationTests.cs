// =============================================================================
// <copyright file="PatientEndpointsIntegrationTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Integration;

using Alba;
using Gateway.API.Models;

/// <summary>
/// Integration tests for the patient registration endpoints.
/// Uses Alba to test the full HTTP flow.
/// </summary>
[Category("Integration")]
[ClassDataSource<GatewayAlbaBootstrap>(Shared = SharedType.PerTestSession)]
public sealed class PatientEndpointsIntegrationTests
{
    private const string ApiKeyHeader = "X-API-Key";
    private readonly GatewayAlbaBootstrap _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatientEndpointsIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The Alba bootstrap fixture.</param>
    public PatientEndpointsIntegrationTests(GatewayAlbaBootstrap fixture)
    {
        _fixture = fixture;
    }

    private void AddApiKey(Scenario s) => s.WithRequestHeader(ApiKeyHeader, GatewayAlbaBootstrap.TestApiKey);

    #region POST /api/patients/register

    [Test]
    public async Task PatientRegistration_ValidRequest_CreatesWorkItemAndReturnsId()
    {
        // Arrange
        var request = new RegisterPatientRequest
        {
            PatientId = "integration-patient-001",
            EncounterId = "integration-encounter-001",
            PracticeId = "integration-practice-001"
        };

        // Act - POST /api/patients/register
        var registerResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Post.Json(request).ToUrl("/api/patients/register");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var response = registerResult.ReadAsJson<RegisterPatientResponse>();

        // Assert - response contains workItemId
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.WorkItemId).IsNotNull();
        await Assert.That(response.WorkItemId).IsNotEmpty();

        // Act - GET /api/work-items/{id}
        var getResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Get.Url($"/api/work-items/{response.WorkItemId}");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var workItem = getResult.ReadAsJson<WorkItem>();

        // Assert - work item exists and has Pending status
        await Assert.That(workItem).IsNotNull();
        await Assert.That(workItem!.Status).IsEqualTo(WorkItemStatus.Pending);
        await Assert.That(workItem.PatientId).IsEqualTo("integration-patient-001");
        await Assert.That(workItem.EncounterId).IsEqualTo("integration-encounter-001");
    }

    #endregion

    #region GET /api/patients/{patientId}

    [Test]
    public async Task PatientRegistration_GetPatient_ReturnsRegisteredPatient()
    {
        // Arrange
        var request = new RegisterPatientRequest
        {
            PatientId = "get-patient-001",
            EncounterId = "get-encounter-001",
            PracticeId = "get-practice-001"
        };

        // Act - register patient first
        await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Post.Json(request).ToUrl("/api/patients/register");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        // Act - GET /api/patients/{patientId}
        var getResult = await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Get.Url("/api/patients/get-patient-001");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var patient = getResult.ReadAsJson<RegisteredPatient>();

        // Assert
        await Assert.That(patient).IsNotNull();
        await Assert.That(patient!.PatientId).IsEqualTo("get-patient-001");
        await Assert.That(patient.PracticeId).IsEqualTo("get-practice-001");
    }

    #endregion

    #region DELETE /api/patients/{patientId}

    [Test]
    public async Task PatientRegistration_UnregisterPatient_Returns200AndPatientNotFound()
    {
        // Arrange
        var request = new RegisterPatientRequest
        {
            PatientId = "unregister-patient-001",
            EncounterId = "unregister-encounter-001",
            PracticeId = "unregister-practice-001"
        };

        await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Post.Json(request).ToUrl("/api/patients/register");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        // Act - DELETE /api/patients/{patientId}
        await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Delete.Url("/api/patients/unregister-patient-001");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        // Assert - patient no longer exists (GET returns 404)
        await _fixture.Host.Scenario(s =>
        {
            AddApiKey(s);
            s.Get.Url("/api/patients/unregister-patient-001");
            s.StatusCodeShouldBe(404);
        }).ConfigureAwait(false);
    }

    #endregion
}
