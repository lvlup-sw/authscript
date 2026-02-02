// =============================================================================
// <copyright file="EncounterProcessingIntegrationTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Integration;

using Alba;
using Gateway.API.Contracts;
using Gateway.API.Models;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Integration tests for the encounter processing flow.
/// Tests the full flow from patient registration through status update.
/// </summary>
[Category("Integration")]
[ClassDataSource<EncounterProcessingAlbaBootstrap>(Shared = SharedType.PerTestSession)]
public sealed class EncounterProcessingIntegrationTests
{
    private const string ApiKeyHeader = "X-API-Key";
    private readonly EncounterProcessingAlbaBootstrap _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncounterProcessingIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The Alba bootstrap fixture.</param>
    public EncounterProcessingIntegrationTests(EncounterProcessingAlbaBootstrap fixture)
    {
        _fixture = fixture;
    }

    private void AddApiKey(Scenario s) => s.WithRequestHeader(ApiKeyHeader, EncounterProcessingAlbaBootstrap.TestApiKey);

    [Test]
    public async Task EncounterCompletion_UpdatesWorkItemStatus()
    {
        // Arrange - Register a patient
        var request = new RegisterPatientRequest
        {
            PatientId = "encounter-test-patient-001",
            EncounterId = "encounter-test-encounter-001",
            PracticeId = "encounter-test-practice-001"
        };

        var registerResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Post
                .Json(request)
                .ToUrl("/api/patients/register");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var registerResponse = registerResult.ReadAsJson<RegisterPatientResponse>();
        await Assert.That(registerResponse).IsNotNull();
        var workItemId = registerResponse!.WorkItemId;

        // Verify initial state is Pending
        var initialGetResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Get.Url($"/api/work-items/{workItemId}");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var initialWorkItem = initialGetResult.ReadAsJson<WorkItem>();
        await Assert.That(initialWorkItem).IsNotNull();
        await Assert.That(initialWorkItem!.Status).IsEqualTo(WorkItemStatus.Pending);

        // Act - Get the EncounterProcessor from DI and call ProcessAsync directly
        // This simulates what would happen after polling detects encounter completion
        // Note: IEncounterProcessor is scoped, so we need to create a scope
        using (var scope = _fixture.Host.Services.CreateScope())
        {
            var encounterProcessor = scope.ServiceProvider.GetRequiredService<IEncounterProcessor>();
            var evt = new EncounterCompletedEvent
            {
                PatientId = request.PatientId,
                EncounterId = request.EncounterId,
                PracticeId = request.PracticeId,
                WorkItemId = workItemId
            };

            await encounterProcessor.ProcessAsync(evt, CancellationToken.None).ConfigureAwait(false);
        }

        // Assert - Work item status should be updated (not Pending anymore)
        var getResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Get.Url($"/api/work-items/{workItemId}");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var workItem = getResult.ReadAsJson<WorkItem>();
        await Assert.That(workItem).IsNotNull();

        // Status should have changed from Pending to something else
        // The stub IntelligenceClient returns "APPROVE" which maps to ReadyForReview
        await Assert.That(workItem!.Status).IsNotEqualTo(WorkItemStatus.Pending);
        await Assert.That(workItem.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    [Test]
    public async Task EncounterCompletion_WorkItemHasProcedureCode()
    {
        // Arrange - Register a patient
        var request = new RegisterPatientRequest
        {
            PatientId = "sr-test-patient-001",
            EncounterId = "sr-test-encounter-001",
            PracticeId = "sr-test-practice-001"
        };

        var registerResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Post
                .Json(request)
                .ToUrl("/api/patients/register");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var registerResponse = registerResult.ReadAsJson<RegisterPatientResponse>();
        var workItemId = registerResponse!.WorkItemId;

        // Act - Process the encounter (using scope for scoped service)
        using (var scope = _fixture.Host.Services.CreateScope())
        {
            var encounterProcessor = scope.ServiceProvider.GetRequiredService<IEncounterProcessor>();
            var evt = new EncounterCompletedEvent
            {
                PatientId = request.PatientId,
                EncounterId = request.EncounterId,
                PracticeId = request.PracticeId,
                WorkItemId = workItemId
            };

            await encounterProcessor.ProcessAsync(evt, CancellationToken.None).ConfigureAwait(false);
        }

        // Assert - Work item should have procedure code (set by processor from IntelligenceClient)
        var getResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Get.Url($"/api/work-items/{workItemId}");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var workItem = getResult.ReadAsJson<WorkItem>();
        await Assert.That(workItem).IsNotNull();

        // The stub IntelligenceClient returns "72148" as the procedure code
        await Assert.That(workItem!.ProcedureCode).IsNotNull();
        await Assert.That(workItem.ProcedureCode).IsEqualTo("72148");
    }

    [Test]
    public async Task EncounterCompletion_WorkItemHasServiceRequestId()
    {
        // Arrange - Register a patient
        var request = new RegisterPatientRequest
        {
            PatientId = "srid-test-patient-001",
            EncounterId = "srid-test-encounter-001",
            PracticeId = "srid-test-practice-001"
        };

        var registerResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Post
                .Json(request)
                .ToUrl("/api/patients/register");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var registerResponse = registerResult.ReadAsJson<RegisterPatientResponse>();
        var workItemId = registerResponse!.WorkItemId;

        // Act - Process the encounter (using scope for scoped service)
        using (var scope = _fixture.Host.Services.CreateScope())
        {
            var encounterProcessor = scope.ServiceProvider.GetRequiredService<IEncounterProcessor>();
            var evt = new EncounterCompletedEvent
            {
                PatientId = request.PatientId,
                EncounterId = request.EncounterId,
                PracticeId = request.PracticeId,
                WorkItemId = workItemId
            };

            await encounterProcessor.ProcessAsync(evt, CancellationToken.None).ConfigureAwait(false);
        }

        // Assert - Work item should have ServiceRequestId (from the mock clinical bundle)
        var getResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Get.Url($"/api/work-items/{workItemId}");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var workItem = getResult.ReadAsJson<WorkItem>();
        await Assert.That(workItem).IsNotNull();

        // The mock aggregator returns a service request with ID "test-service-request-001"
        await Assert.That(workItem!.ServiceRequestId).IsNotNull();
        await Assert.That(workItem.ServiceRequestId).IsEqualTo("test-service-request-001");
    }

    [Test]
    public async Task EncounterCompletion_WorkItemHasUpdatedAt()
    {
        // Arrange - Register a patient
        var request = new RegisterPatientRequest
        {
            PatientId = "updated-test-patient-001",
            EncounterId = "updated-test-encounter-001",
            PracticeId = "updated-test-practice-001"
        };

        var registerResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Post
                .Json(request)
                .ToUrl("/api/patients/register");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var registerResponse = registerResult.ReadAsJson<RegisterPatientResponse>();
        var workItemId = registerResponse!.WorkItemId;

        // Act - Process the encounter (using scope for scoped service)
        var beforeProcessing = DateTimeOffset.UtcNow;
        using (var scope = _fixture.Host.Services.CreateScope())
        {
            var encounterProcessor = scope.ServiceProvider.GetRequiredService<IEncounterProcessor>();
            var evt = new EncounterCompletedEvent
            {
                PatientId = request.PatientId,
                EncounterId = request.EncounterId,
                PracticeId = request.PracticeId,
                WorkItemId = workItemId
            };

            await encounterProcessor.ProcessAsync(evt, CancellationToken.None).ConfigureAwait(false);
        }

        // Assert - Work item should have UpdatedAt timestamp
        var getResult = await _fixture.Host.Scenario(scenario =>
        {
            AddApiKey(scenario);
            scenario.Get.Url($"/api/work-items/{workItemId}");
            scenario.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var workItem = getResult.ReadAsJson<WorkItem>();
        await Assert.That(workItem).IsNotNull();
        await Assert.That(workItem!.UpdatedAt).IsNotNull();
        await Assert.That(workItem.UpdatedAt!.Value).IsGreaterThanOrEqualTo(beforeProcessing);
    }
}
