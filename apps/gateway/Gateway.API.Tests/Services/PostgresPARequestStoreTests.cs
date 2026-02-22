// =============================================================================
// <copyright file="PostgresPARequestStoreTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Services;

using Gateway.API.Data;
using Gateway.API.GraphQL.Models;
using Gateway.API.Services;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Tests for PostgresPARequestStore CRUD operations using EF Core InMemory database.
/// </summary>
public class PostgresPARequestStoreTests
{
    private static GatewayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new GatewayDbContext(options);
    }

    private static PostgresPARequestStore CreateStore(GatewayDbContext context)
    {
        return new PostgresPARequestStore(context);
    }

    private static PARequestModel CreateSampleRequest(
        string status = "draft",
        string patientName = "John Doe",
        string procedureCode = "72148",
        string procedureName = "MRI Lumbar Spine",
        IReadOnlyList<CriterionModel>? criteria = null)
    {
        return new PARequestModel
        {
            Id = string.Empty,
            PatientId = "60178",
            Patient = new PatientModel
            {
                Id = "60178",
                Name = patientName,
                Mrn = "MRN-001",
                Dob = "1990-01-15",
                MemberId = "MEM-123",
                Payer = "Aetna",
                Address = "123 Main St",
                Phone = "555-0100",
            },
            ProcedureCode = procedureCode,
            ProcedureName = procedureName,
            Diagnosis = "Low back pain",
            DiagnosisCode = "M54.5",
            Payer = "Aetna",
            Provider = "Dr. Smith",
            ProviderNpi = "1234567890",
            ServiceDate = "2026-03-01",
            PlaceOfService = "Office",
            ClinicalSummary = string.Empty,
            Status = status,
            Confidence = 0,
            CreatedAt = DateTimeOffset.UtcNow.ToString("o"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("o"),
            ReviewTimeSeconds = 0,
            Criteria = criteria ?? [],
        };
    }

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_PersistsRequest_ReturnsWithGeneratedId()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var request = CreateSampleRequest();

        // Act
        var result = await store.CreateAsync(request, "a-195900.E-60178");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo("PA-001");

        var saved = await context.PriorAuthRequests.FindAsync("PA-001");
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.PatientName).IsEqualTo("John Doe");
        await Assert.That(saved.FhirPatientId).IsEqualTo("a-195900.E-60178");
    }

    [Test]
    public async Task CreateAsync_WithCriteria_PersistsCriteriaAsJson()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var criteria = new List<CriterionModel>
        {
            new() { Label = "Medical necessity", Met = true, Reason = "Documented" },
            new() { Label = "Conservative treatment", Met = false, Reason = "Not attempted" },
        };
        var request = CreateSampleRequest(criteria: criteria);

        // Act
        var result = await store.CreateAsync(request, "fhir-1");

        // Assert
        var saved = await context.PriorAuthRequests.FindAsync(result.Id);
        await Assert.That(saved).IsNotNull();
        await Assert.That(saved!.CriteriaJson).IsNotNull();
        await Assert.That(saved.CriteriaJson!).Contains("Medical necessity");
        await Assert.That(saved.CriteriaJson!).Contains("Conservative treatment");
    }

    [Test]
    public async Task CreateAsync_MultipleRequests_GeneratesSequentialIds()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var r1 = await store.CreateAsync(CreateSampleRequest(), "fhir-1");
        var r2 = await store.CreateAsync(CreateSampleRequest(), "fhir-2");
        var r3 = await store.CreateAsync(CreateSampleRequest(), "fhir-3");

        // Assert
        await Assert.That(r1.Id).IsEqualTo("PA-001");
        await Assert.That(r2.Id).IsEqualTo("PA-002");
        await Assert.That(r3.Id).IsEqualTo("PA-003");
    }

    #endregion

    #region GetByIdAsync Tests

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsRequest()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var created = await store.CreateAsync(CreateSampleRequest(), "fhir-1");

        // Act
        var result = await store.GetByIdAsync(created.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo("PA-001");
        await Assert.That(result.Patient.Name).IsEqualTo("John Doe");
        await Assert.That(result.ProcedureCode).IsEqualTo("72148");
        await Assert.That(result.Status).IsEqualTo("draft");
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var result = await store.GetByIdAsync("PA-999");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetByIdAsync_WithCriteria_DeserializesFromJson()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var criteria = new List<CriterionModel>
        {
            new() { Label = "Medical necessity", Met = true, Reason = "Documented" },
        };
        var created = await store.CreateAsync(CreateSampleRequest(criteria: criteria), "fhir-1");

        // Act
        var result = await store.GetByIdAsync(created.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Criteria.Count).IsEqualTo(1);
        await Assert.That(result.Criteria[0].Label).IsEqualTo("Medical necessity");
        await Assert.That(result.Criteria[0].Met).IsEqualTo(true);
        await Assert.That(result.Criteria[0].Reason).IsEqualTo("Documented");
    }

    #endregion

    #region GetAllAsync Tests

    [Test]
    public async Task GetAllAsync_ReturnsAllRequests_OrderedByCreatedAtDesc()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        var r1 = await store.CreateAsync(CreateSampleRequest(patientName: "First"), "fhir-1");
        await Task.Delay(50);
        var r2 = await store.CreateAsync(CreateSampleRequest(patientName: "Second"), "fhir-2");
        await Task.Delay(50);
        var r3 = await store.CreateAsync(CreateSampleRequest(patientName: "Third"), "fhir-3");

        // Act
        var results = await store.GetAllAsync();

        // Assert
        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results[0].Patient.Name).IsEqualTo("Third");
        await Assert.That(results[1].Patient.Name).IsEqualTo("Second");
        await Assert.That(results[2].Patient.Name).IsEqualTo("First");
    }

    #endregion

    #region UpdateFieldsAsync Tests

    [Test]
    public async Task UpdateFieldsAsync_UpdatesDiagnosisAndCriteria()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var created = await store.CreateAsync(CreateSampleRequest(), "fhir-1");
        var newCriteria = new List<CriterionModel>
        {
            new() { Label = "Updated criterion", Met = true },
        };

        // Act
        var result = await store.UpdateFieldsAsync(
            created.Id,
            diagnosis: "Updated diagnosis",
            diagnosisCode: "M79.3",
            serviceDate: null,
            placeOfService: null,
            clinicalSummary: "Updated summary",
            criteria: newCriteria);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Diagnosis).IsEqualTo("Updated diagnosis");
        await Assert.That(result.DiagnosisCode).IsEqualTo("M79.3");
        await Assert.That(result.ClinicalSummary).IsEqualTo("Updated summary");
        await Assert.That(result.Criteria.Count).IsEqualTo(1);
        await Assert.That(result.Criteria[0].Label).IsEqualTo("Updated criterion");
        // Null fields should not be changed
        await Assert.That(result.ServiceDate).IsEqualTo("2026-03-01");
        await Assert.That(result.PlaceOfService).IsEqualTo("Office");
    }

    [Test]
    public async Task UpdateFieldsAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var result = await store.UpdateFieldsAsync(
            "PA-999",
            diagnosis: "Updated",
            diagnosisCode: null,
            serviceDate: null,
            placeOfService: null,
            clinicalSummary: null,
            criteria: null);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region ApplyAnalysisResultAsync Tests

    [Test]
    public async Task ApplyAnalysisResultAsync_SetsReadyStatusAndConfidence()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var created = await store.CreateAsync(CreateSampleRequest(), "fhir-1");
        var criteria = new List<CriterionModel>
        {
            new() { Label = "Medical necessity", Met = true, Reason = "Documented pain" },
        };

        // Act
        var result = await store.ApplyAnalysisResultAsync(
            created.Id,
            clinicalSummary: "AI-generated summary",
            confidence: 85,
            criteria: criteria);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Status).IsEqualTo("ready");
        await Assert.That(result.Confidence).IsEqualTo(85);
        await Assert.That(result.ClinicalSummary).IsEqualTo("AI-generated summary");
        await Assert.That(result.Criteria.Count).IsEqualTo(1);
        await Assert.That(result.ReadyAt).IsNotNull();
    }

    #endregion

    #region SubmitAsync Tests

    [Test]
    public async Task SubmitAsync_SetsWaitingForInsuranceStatus()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var created = await store.CreateAsync(CreateSampleRequest(status: "ready"), "fhir-1");

        // Act
        var result = await store.SubmitAsync(created.Id);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Status).IsEqualTo("waiting_for_insurance");
        await Assert.That(result.SubmittedAt).IsNotNull();
    }

    [Test]
    public async Task SubmitAsync_AddsReviewTimeSeconds()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var created = await store.CreateAsync(CreateSampleRequest(status: "ready"), "fhir-1");

        // Act
        var result = await store.SubmitAsync(created.Id, addReviewTimeSeconds: 120);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ReviewTimeSeconds).IsEqualTo(120);
    }

    #endregion

    #region AddReviewTimeAsync Tests

    [Test]
    public async Task AddReviewTimeAsync_IncrementsSeconds()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var created = await store.CreateAsync(CreateSampleRequest(), "fhir-1");

        // Act
        await store.AddReviewTimeAsync(created.Id, 30);
        var result = await store.AddReviewTimeAsync(created.Id, 45);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ReviewTimeSeconds).IsEqualTo(75);
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);
        var created = await store.CreateAsync(CreateSampleRequest(), "fhir-1");

        // Act
        var result = await store.DeleteAsync(created.Id);

        // Assert
        await Assert.That(result).IsTrue();
        var afterDelete = await store.GetByIdAsync(created.Id);
        await Assert.That(afterDelete).IsNull();
    }

    [Test]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var result = await store.DeleteAsync("PA-999");

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    #region GetStatsAsync Tests

    [Test]
    public async Task GetStatsAsync_CountsByStatus()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        await store.CreateAsync(CreateSampleRequest(status: "draft"), "fhir-1");
        await store.CreateAsync(CreateSampleRequest(status: "ready"), "fhir-2");
        await store.CreateAsync(CreateSampleRequest(status: "ready"), "fhir-3");
        await store.CreateAsync(CreateSampleRequest(status: "waiting_for_insurance"), "fhir-4");
        await store.CreateAsync(CreateSampleRequest(status: "approved"), "fhir-5");
        await store.CreateAsync(CreateSampleRequest(status: "denied"), "fhir-6");

        // Act
        var stats = await store.GetStatsAsync();

        // Assert
        await Assert.That(stats.Ready).IsEqualTo(2);
        await Assert.That(stats.WaitingForInsurance).IsEqualTo(1);
        await Assert.That(stats.Submitted).IsEqualTo(2); // approved + denied
        await Assert.That(stats.Attention).IsEqualTo(1); // draft
        await Assert.That(stats.Total).IsEqualTo(6);
    }

    [Test]
    public async Task GetStatsAsync_EmptyDatabase_ReturnsZeros()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var stats = await store.GetStatsAsync();

        // Assert
        await Assert.That(stats.Ready).IsEqualTo(0);
        await Assert.That(stats.Submitted).IsEqualTo(0);
        await Assert.That(stats.WaitingForInsurance).IsEqualTo(0);
        await Assert.That(stats.Attention).IsEqualTo(0);
        await Assert.That(stats.Total).IsEqualTo(0);
    }

    #endregion

    #region GetActivityAsync Tests

    [Test]
    public async Task GetActivityAsync_ReturnsRecentUpdatesOrderedByUpdatedAtDesc()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        var r1 = await store.CreateAsync(CreateSampleRequest(status: "draft", patientName: "Alice"), "fhir-1");
        await Task.Delay(50);
        var r2 = await store.CreateAsync(CreateSampleRequest(status: "ready", patientName: "Bob"), "fhir-2");
        await Task.Delay(50);
        var r3 = await store.CreateAsync(CreateSampleRequest(status: "waiting_for_insurance", patientName: "Charlie"), "fhir-3");

        // Act
        var activity = await store.GetActivityAsync();

        // Assert
        await Assert.That(activity.Count).IsEqualTo(3);
        await Assert.That(activity[0].PatientName).IsEqualTo("Charlie");
        await Assert.That(activity[0].Action).IsEqualTo("PA submitted");
        await Assert.That(activity[0].Type).IsEqualTo("success");
        await Assert.That(activity[1].PatientName).IsEqualTo("Bob");
        await Assert.That(activity[1].Action).IsEqualTo("Ready for review");
        await Assert.That(activity[1].Type).IsEqualTo("ready");
        await Assert.That(activity[2].PatientName).IsEqualTo("Alice");
        await Assert.That(activity[2].Action).IsEqualTo("Updated");
        await Assert.That(activity[2].Type).IsEqualTo("info");
    }

    [Test]
    public async Task GetActivityAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateContext();
        var store = CreateStore(context);

        // Act
        var activity = await store.GetActivityAsync();

        // Assert
        await Assert.That(activity).IsNotNull();
        await Assert.That(activity.Count).IsEqualTo(0);
    }

    #endregion
}
