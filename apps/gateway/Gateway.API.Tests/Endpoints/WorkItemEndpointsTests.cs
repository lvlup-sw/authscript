using Gateway.API.Contracts;
using Gateway.API.Endpoints;
using Gateway.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gateway.API.Tests.Endpoints;

/// <summary>
/// Tests for the WorkItemEndpoints API endpoints.
/// </summary>
public sealed class WorkItemEndpointsTests
{
    private readonly IWorkItemStore _workItemStore;
    private readonly IFhirDataAggregator _fhirAggregator;
    private readonly IIntelligenceClient _intelligenceClient;
    private readonly ILogger<RehydrateResponse> _logger;

    public WorkItemEndpointsTests()
    {
        _workItemStore = Substitute.For<IWorkItemStore>();
        _fhirAggregator = Substitute.For<IFhirDataAggregator>();
        _intelligenceClient = Substitute.For<IIntelligenceClient>();
        _logger = Substitute.For<ILogger<RehydrateResponse>>();
    }

    private static WorkItem CreateTestWorkItem(
        string id = "wi-001",
        string patientId = "patient-123",
        string procedureCode = "72148")
    {
        return new WorkItem
        {
            Id = id,
            EncounterId = "encounter-456",
            PatientId = patientId,
            ServiceRequestId = "sr-789",
            Status = WorkItemStatus.MissingData,
            ProcedureCode = procedureCode,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static ClinicalBundle CreateTestClinicalBundle(string patientId = "patient-123")
    {
        return new ClinicalBundle
        {
            PatientId = patientId,
            Patient = new PatientInfo
            {
                Id = patientId,
                GivenName = "John",
                FamilyName = "Doe",
                BirthDate = new DateOnly(1985, 3, 15)
            },
            Conditions =
            [
                new ConditionInfo
                {
                    Id = "cond-001",
                    Code = "M54.5",
                    Display = "Low back pain",
                    ClinicalStatus = "active",
                    OnsetDate = new DateOnly(2024, 1, 15)
                }
            ]
        };
    }

    private static PAFormData CreateTestFormData()
    {
        return new PAFormData
        {
            PatientName = "John Doe",
            PatientDob = "1985-03-15",
            MemberId = "MEM123456",
            DiagnosisCodes = ["M54.5"],
            ProcedureCode = "72148",
            ClinicalSummary = "Patient presents with chronic low back pain...",
            SupportingEvidence =
            [
                new EvidenceItem
                {
                    CriterionId = "clinical_indication",
                    Status = "met",
                    Evidence = "Chronic low back pain",
                    Source = "Progress Note",
                    Confidence = 0.95
                }
            ],
            Recommendation = "approve",
            ConfidenceScore = 0.92,
            FieldMappings = new Dictionary<string, string>
            {
                ["patient_name"] = "John Doe",
                ["dob"] = "1985-03-15"
            }
        };
    }

    #region Rehydrate Endpoint Tests

    [Test]
    public async Task RehydrateEndpoint_ValidWorkItem_ReturnsOk()
    {
        // Arrange
        const string workItemId = "wi-001";
        var workItem = CreateTestWorkItem(workItemId);
        var clinicalBundle = CreateTestClinicalBundle();
        var formData = CreateTestFormData();

        _workItemStore
            .GetByIdAsync(workItemId, Arg.Any<CancellationToken>())
            .Returns(workItem);

        _fhirAggregator
            .AggregateClinicalDataAsync(workItem.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(clinicalBundle);

        _intelligenceClient
            .AnalyzeAsync(clinicalBundle, workItem.ProcedureCode, Arg.Any<CancellationToken>())
            .Returns(formData);

        _workItemStore
            .UpdateStatusAsync(workItemId, Arg.Any<WorkItemStatus>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await InvokeRehydrateAsync(workItemId);

        // Assert
        await Assert.That(result).IsNotNull();
        var okResult = result as Ok<RehydrateResponse>;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(okResult!.Value).IsNotNull();
        await Assert.That(okResult.Value!.WorkItemId).IsEqualTo(workItemId);
        await Assert.That(okResult.Value.NewStatus).IsNotNull();
    }

    [Test]
    public async Task RehydrateEndpoint_NotFoundWorkItem_Returns404()
    {
        // Arrange
        const string workItemId = "wi-nonexistent";

        _workItemStore
            .GetByIdAsync(workItemId, Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        // Act
        var result = await InvokeRehydrateAsync(workItemId);

        // Assert
        await Assert.That(result).IsNotNull();
        var notFoundResult = result as NotFound<ErrorResponse>;
        await Assert.That(notFoundResult).IsNotNull();
        await Assert.That(notFoundResult!.Value).IsNotNull();
        await Assert.That(notFoundResult.Value!.Message).Contains("not found");
        await Assert.That(notFoundResult.Value.Code).IsEqualTo("WORK_ITEM_NOT_FOUND");
    }

    [Test]
    public async Task RehydrateEndpoint_TriggersReanalysis_UpdatesWorkItem()
    {
        // Arrange
        const string workItemId = "wi-001";
        var workItem = CreateTestWorkItem(workItemId);
        var clinicalBundle = CreateTestClinicalBundle();
        var formData = CreateTestFormData();

        _workItemStore
            .GetByIdAsync(workItemId, Arg.Any<CancellationToken>())
            .Returns(workItem);

        _fhirAggregator
            .AggregateClinicalDataAsync(workItem.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(clinicalBundle);

        _intelligenceClient
            .AnalyzeAsync(clinicalBundle, workItem.ProcedureCode, Arg.Any<CancellationToken>())
            .Returns(formData);

        _workItemStore
            .UpdateStatusAsync(workItemId, Arg.Any<WorkItemStatus>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await InvokeRehydrateAsync(workItemId);

        // Assert
        // Verify IFhirDataAggregator.AggregateClinicalDataAsync was called with encounterId
        await _fhirAggregator.Received(1).AggregateClinicalDataAsync(
            workItem.PatientId,
            workItem.EncounterId,
            Arg.Any<CancellationToken>());

        // Verify IIntelligenceClient.AnalyzeAsync was called with clinical bundle
        await _intelligenceClient.Received(1).AnalyzeAsync(
            clinicalBundle,
            workItem.ProcedureCode,
            Arg.Any<CancellationToken>());

        // Verify IWorkItemStore.UpdateStatusAsync was called
        await _workItemStore.Received(1).UpdateStatusAsync(
            workItemId,
            Arg.Any<WorkItemStatus>(),
            Arg.Any<CancellationToken>());

        // Result should be OK
        var okResult = result as Ok<RehydrateResponse>;
        await Assert.That(okResult).IsNotNull();
    }

    private async Task<IResult> InvokeRehydrateAsync(string id)
    {
        return await WorkItemEndpoints.RehydrateAsync(
            id,
            _workItemStore,
            _fhirAggregator,
            _intelligenceClient,
            _logger,
            CancellationToken.None);
    }

    #endregion

    #region Delete Endpoint Tests

    [Test]
    public async Task DeleteAsync_ExistingWorkItem_ReturnsOk()
    {
        // Arrange
        const string workItemId = "wi-delete";

        // Act
        var result = await WorkItemEndpoints.DeleteAsync(
            workItemId,
            _workItemStore,
            CancellationToken.None);

        // Assert
        await Assert.That(result).IsTypeOf<Ok>();
        await _workItemStore.Received(1).DeleteAsync(workItemId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_NonExistentWorkItem_ReturnsOk()
    {
        // Arrange - idempotent: returns OK even if not found
        const string workItemId = "wi-nonexistent";

        // Act
        var result = await WorkItemEndpoints.DeleteAsync(
            workItemId,
            _workItemStore,
            CancellationToken.None);

        // Assert
        await Assert.That(result).IsTypeOf<Ok>();
    }

    #endregion
}
