// =============================================================================
// <copyright file="PatientEndpointsTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Endpoints;

using Gateway.API.Contracts;
using Gateway.API.Endpoints;
using Gateway.API.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

/// <summary>
/// Tests for the PatientEndpoints API endpoints.
/// </summary>
public sealed class PatientEndpointsTests
{
    private readonly IWorkItemStore _workItemStore;
    private readonly IPatientRegistry _patientRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatientEndpointsTests"/> class.
    /// </summary>
    public PatientEndpointsTests()
    {
        _workItemStore = Substitute.For<IWorkItemStore>();
        _patientRegistry = Substitute.For<IPatientRegistry>();
    }

    [Test]
    public async Task RegisterAsync_ValidRequest_CreatesWorkItemInPendingStatus()
    {
        // Arrange
        var request = new RegisterPatientRequest
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789"
        };

        WorkItem? capturedWorkItem = null;
        _workItemStore.CreateAsync(Arg.Do<WorkItem>(wi => capturedWorkItem = wi), Arg.Any<CancellationToken>())
            .Returns("workitem-id-1");

        // Act
        await PatientEndpoints.RegisterAsync(request, _workItemStore, _patientRegistry);

        // Assert
        await Assert.That(capturedWorkItem).IsNotNull();
        await Assert.That(capturedWorkItem!.Status).IsEqualTo(WorkItemStatus.Pending);
        await Assert.That(capturedWorkItem.PatientId).IsEqualTo("patient-123");
        await Assert.That(capturedWorkItem.EncounterId).IsEqualTo("encounter-456");
        await Assert.That(capturedWorkItem.ServiceRequestId).IsNull();
        await Assert.That(capturedWorkItem.ProcedureCode).IsNull();
    }

    [Test]
    public async Task RegisterAsync_ValidRequest_RegistersPatientWithWorkItemId()
    {
        // Arrange
        var request = new RegisterPatientRequest
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789"
        };

        _workItemStore.CreateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns("workitem-id-1");

        RegisteredPatient? capturedPatient = null;
        _patientRegistry.RegisterAsync(Arg.Do<RegisteredPatient>(p => capturedPatient = p), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await PatientEndpoints.RegisterAsync(request, _workItemStore, _patientRegistry);

        // Assert
        await Assert.That(capturedPatient).IsNotNull();
        await Assert.That(capturedPatient!.PatientId).IsEqualTo("patient-123");
        await Assert.That(capturedPatient.EncounterId).IsEqualTo("encounter-456");
        await Assert.That(capturedPatient.PracticeId).IsEqualTo("practice-789");
        await Assert.That(capturedPatient.WorkItemId).IsEqualTo("workitem-id-1");
    }

    [Test]
    public async Task RegisterAsync_ValidRequest_ReturnsWorkItemId()
    {
        // Arrange
        var request = new RegisterPatientRequest
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789"
        };

        _workItemStore.CreateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns("workitem-id-1");

        // Act
        var result = await PatientEndpoints.RegisterAsync(request, _workItemStore, _patientRegistry);

        // Assert
        await Assert.That(result.Result).IsTypeOf<Ok<RegisterPatientResponse>>();
        var okResult = result.Result as Ok<RegisterPatientResponse>;
        await Assert.That(okResult!.Value!.WorkItemId).IsEqualTo("workitem-id-1");
        await Assert.That(okResult.Value!.Message).Contains("registered");
    }
}
