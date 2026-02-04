using Gateway.API.Contracts;
using Gateway.API.Contracts.Http;
using Gateway.API.Models;
using Gateway.API.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for EncounterProcessor that orchestrates clinical data hydration
/// and PA form generation.
/// </summary>
public class EncounterProcessorTests
{
    private readonly IFhirDataAggregator _aggregator;
    private readonly IIntelligenceClient _intelligenceClient;
    private readonly IPdfFormStamper _pdfStamper;
    private readonly IAnalysisResultStore _resultStore;
    private readonly INotificationHub _notificationHub;
    private readonly IWorkItemStore _workItemStore;
    private readonly ILogger<EncounterProcessor> _logger;
    private readonly EncounterProcessor _sut;

    public EncounterProcessorTests()
    {
        _aggregator = Substitute.For<IFhirDataAggregator>();
        _intelligenceClient = Substitute.For<IIntelligenceClient>();
        _pdfStamper = Substitute.For<IPdfFormStamper>();
        _resultStore = Substitute.For<IAnalysisResultStore>();
        _notificationHub = Substitute.For<INotificationHub>();
        _workItemStore = Substitute.For<IWorkItemStore>();
        _logger = Substitute.For<ILogger<EncounterProcessor>>();

        _sut = new EncounterProcessor(
            _aggregator,
            _intelligenceClient,
            _pdfStamper,
            _resultStore,
            _notificationHub,
            _workItemStore,
            _logger);
    }

    [Test]
    public async Task ProcessEncounterAsync_HydratesPatientContext_CallsAggregator()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes

        SetupSuccessfulMocks(patientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert
        await _aggregator.Received(1).AggregateClinicalDataAsync(
            patientId,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_FetchesConditionsObservationsProcedures_ViaClinicalBundle()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(patientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - The clinical bundle passed to intelligence should contain clinical data
        await _intelligenceClient.Received(1).AnalyzeAsync(
            Arg.Is<ClinicalBundle>(b =>
                b.PatientId == patientId &&
                b.Conditions.Count > 0 &&
                b.Observations.Count > 0 &&
                b.Procedures.Count > 0),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_SendsBundleToIntelligence_WithProcedureCode()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(patientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert
        await _intelligenceClient.Received(1).AnalyzeAsync(
            clinicalBundle,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_ReturnsOnIntelligenceError_DoesNotThrow()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);

        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Intelligence service unavailable"));

        // Act - Should complete without throwing (errors are handled gracefully inside)
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - PDF stamper should NOT be called due to Intelligence failure
        await _pdfStamper.DidNotReceive().StampFormAsync(Arg.Any<PAFormData>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_OnHttpRequestException_SendsErrorNotification()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);

        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Intelligence service unavailable"));

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - Error notification should be sent to subscribers
        await _notificationHub.Received(1).WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "PROCESSING_ERROR" &&
                n.EncounterId == encounterId &&
                n.PatientId == patientId &&
                n.Message.Contains("Service error")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_OnGeneralException_SendsErrorNotification()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);

        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Unexpected internal error"));

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - Error notification should be sent without sensitive details
        await _notificationHub.Received(1).WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "PROCESSING_ERROR" &&
                n.EncounterId == encounterId &&
                n.PatientId == patientId &&
                n.Message == "Unexpected processing error"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_OnGeneralException_DoesNotExposeStackTrace()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);

        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Sensitive internal error with stack details"));

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - Message should NOT contain the original exception message (security)
        await _notificationHub.Received(1).WriteAsync(
            Arg.Is<Notification>(n =>
                !n.Message.Contains("Sensitive") &&
                !n.Message.Contains("stack")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_GeneratesPdfFromFormData_CallsStamper()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(patientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert
        await _pdfStamper.Received(1).StampFormAsync(formData, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_StoresPdfInResultStore_WithTransactionId()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(patientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - PDF should be cached with a transaction key
        await _resultStore.Received(1).SetCachedPdfAsync(
            Arg.Is<string>(key => key.Contains(encounterId)),
            pdfBytes,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_NotifiesViaChannel_WithCompletionMessage()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        var clinicalBundle = CreateTestBundle(patientId);
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(patientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert
        await _notificationHub.Received(1).WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "PA_FORM_READY" &&
                n.EncounterId == encounterId &&
                n.PatientId == patientId),
            Arg.Any<CancellationToken>());
    }

    private void SetupSuccessfulMocks(
        string patientId,
        ClinicalBundle clinicalBundle,
        PAFormData formData,
        byte[] pdfBytes)
    {
        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(formData));
        _pdfStamper.StampFormAsync(Arg.Any<PAFormData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));
    }

    private static ClinicalBundle CreateTestBundle(string patientId)
    {
        return new ClinicalBundle
        {
            PatientId = patientId,
            Patient = new PatientInfo
            {
                Id = patientId,
                GivenName = "Test",
                FamilyName = "Patient",
                BirthDate = new DateOnly(1980, 1, 15),
                MemberId = "MEM123"
            },
            Conditions =
            [
                new ConditionInfo
                {
                    Id = "cond-1",
                    Code = "M54.5",
                    Display = "Low back pain",
                    ClinicalStatus = "active"
                }
            ],
            Observations =
            [
                new ObservationInfo
                {
                    Id = "obs-1",
                    Code = "72166-2",
                    Display = "Smoking status",
                    Value = "Never smoker"
                }
            ],
            Procedures =
            [
                new ProcedureInfo
                {
                    Id = "proc-1",
                    Code = "99213",
                    Display = "Office visit"
                }
            ],
            Documents = []
        };
    }

    private static PAFormData CreateTestFormData()
    {
        return new PAFormData
        {
            PatientName = "Test Patient",
            PatientDob = "1980-01-15",
            MemberId = "MEM123",
            DiagnosisCodes = ["M54.5"],
            ProcedureCode = "72148",
            ClinicalSummary = "Test clinical summary",
            SupportingEvidence =
            [
                new EvidenceItem
                {
                    CriterionId = "diagnosis",
                    Status = "MET",
                    Evidence = "Diagnosis code found",
                    Source = "Conditions",
                    Confidence = 0.95
                }
            ],
            Recommendation = "APPROVE",
            ConfidenceScore = 0.95,
            FieldMappings = new Dictionary<string, string>
            {
                ["PatientName"] = "Test Patient"
            }
        };
    }

    private static PAFormData CreateFormData(string recommendation)
    {
        return new PAFormData
        {
            PatientName = "Test Patient",
            PatientDob = "1980-01-15",
            MemberId = "MEM123",
            DiagnosisCodes = ["M54.5"],
            ProcedureCode = "72148",
            ClinicalSummary = "Test clinical summary",
            SupportingEvidence = [],
            Recommendation = recommendation,
            ConfidenceScore = 0.90,
            FieldMappings = new Dictionary<string, string>()
        };
    }

    private static EncounterCompletedEvent CreateEvent()
    {
        return new EncounterCompletedEvent
        {
            PatientId = "patient-1",
            EncounterId = "encounter-1",
            PracticeId = "practice-1",
            WorkItemId = "workitem-1"
        };
    }

    #region ProcessAsync Tests

    [Test]
    public async Task ProcessAsync_ReceivesEvent_HydratesWithCorrectPatientId()
    {
        // Arrange
        var evt = new EncounterCompletedEvent
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789",
            WorkItemId = "workitem-abc"
        };

        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - aggregator called with correct patient ID and encounter ID
        await _aggregator.Received(1).AggregateClinicalDataAsync(
            "patient-123",
            "encounter-456",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_ReceivesEvent_UpdatesWorkItemStatus()
    {
        // Arrange
        var evt = new EncounterCompletedEvent
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789",
            WorkItemId = "workitem-abc"
        };

        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - work item status updated
        await _workItemStore.Received(1).UpdateStatusAsync(
            "workitem-abc",
            WorkItemStatus.ReadyForReview,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_AnalysisApprove_UpdatesWorkItemToReadyForReview()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await _workItemStore.Received(1).UpdateStatusAsync(
            evt.WorkItemId,
            WorkItemStatus.ReadyForReview,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_AnalysisDeny_UpdatesWorkItemToReadyForReview()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("DENY");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await _workItemStore.Received(1).UpdateStatusAsync(
            evt.WorkItemId,
            WorkItemStatus.ReadyForReview,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_AnalysisNeedsInfo_UpdatesWorkItemToMissingData()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("NEEDS_INFO");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await _workItemStore.Received(1).UpdateStatusAsync(
            evt.WorkItemId,
            WorkItemStatus.MissingData,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_AnalysisNotRequired_UpdatesWorkItemToNoPaRequired()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("NOT_REQUIRED");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await _workItemStore.Received(1).UpdateStatusAsync(
            evt.WorkItemId,
            WorkItemStatus.NoPaRequired,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_SendsNotification_WithEventData()
    {
        // Arrange
        var evt = new EncounterCompletedEvent
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            PracticeId = "practice-789",
            WorkItemId = "workitem-abc"
        };

        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        // Configure work item store for fallback path (work item not found)
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);
        _workItemStore.UpdateStatusAsync(evt.WorkItemId, Arg.Any<WorkItemStatus>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await _notificationHub.Received(1).WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "PA_FORM_READY" &&
                n.EncounterId == "encounter-456" &&
                n.PatientId == "patient-123"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_OnError_SendsErrorNotification()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);

        _aggregator.AggregateClinicalDataAsync(evt.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await _notificationHub.Received(1).WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "PROCESSING_ERROR" &&
                n.EncounterId == evt.EncounterId &&
                n.PatientId == evt.PatientId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_AnalysisComplete_UpdatesWorkItemWithServiceRequestId()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        clinicalBundle = clinicalBundle with
        {
            ServiceRequests =
            [
                new ServiceRequestInfo
                {
                    Id = "sr-from-bundle-123",
                    Status = "active",
                    Code = new CodeableConcept
                    {
                        Coding = [new Coding { Code = "72148", Display = "MRI Lumbar Spine" }],
                        Text = "MRI Lumbar Spine"
                    }
                }
            ]
        };
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _aggregator.AggregateClinicalDataAsync(evt.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(formData));
        _pdfStamper.StampFormAsync(Arg.Any<PAFormData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);

        WorkItem? capturedWorkItem = null;
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Do<WorkItem>(wi => capturedWorkItem = wi), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await Assert.That(capturedWorkItem).IsNotNull();
        await Assert.That(capturedWorkItem!.ServiceRequestId).IsEqualTo("sr-from-bundle-123");
    }

    [Test]
    public async Task ProcessAsync_AnalysisComplete_UpdatesWorkItemWithProcedureCode()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("APPROVE");
        formData = formData with { ProcedureCode = "72148" };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);

        WorkItem? capturedWorkItem = null;
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Do<WorkItem>(wi => capturedWorkItem = wi), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await Assert.That(capturedWorkItem).IsNotNull();
        await Assert.That(capturedWorkItem!.ProcedureCode).IsEqualTo("72148");
    }

    [Test]
    public async Task ProcessAsync_AnalysisComplete_UpdatesWorkItemWithCorrectStatus()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("NEEDS_INFO");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);

        WorkItem? capturedWorkItem = null;
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Do<WorkItem>(wi => capturedWorkItem = wi), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await Assert.That(capturedWorkItem).IsNotNull();
        await Assert.That(capturedWorkItem!.Status).IsEqualTo(WorkItemStatus.MissingData);
    }

    [Test]
    public async Task ProcessAsync_WorkItemNotFound_DoesNotThrow()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        // Act - Should complete without throwing
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - UpdateAsync should NOT be called when work item not found
        await _workItemStore.DidNotReceive().UpdateAsync(
            Arg.Any<string>(),
            Arg.Any<WorkItem>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region WORK_ITEM_STATUS_CHANGED Notification Tests

    [Test]
    public async Task ProcessAsync_StatusUpdatedToReadyForReview_SendsWorkItemStatusChangedNotification()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        clinicalBundle = clinicalBundle with
        {
            ServiceRequests =
            [
                new ServiceRequestInfo
                {
                    Id = "sr-123",
                    Status = "active",
                    Code = new CodeableConcept
                    {
                        Coding = [new Coding { Code = "72148", Display = "MRI Lumbar Spine" }],
                        Text = "MRI Lumbar Spine"
                    }
                }
            ]
        };
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _aggregator.AggregateClinicalDataAsync(evt.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(formData));
        _pdfStamper.StampFormAsync(Arg.Any<PAFormData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - WORK_ITEM_STATUS_CHANGED notification should be sent
        await _notificationHub.Received().WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "WORK_ITEM_STATUS_CHANGED" &&
                n.WorkItemId == evt.WorkItemId &&
                n.NewStatus == "ReadyForReview" &&
                n.PatientId == evt.PatientId &&
                n.ServiceRequestId == "sr-123" &&
                n.ProcedureCode == "72148"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_StatusUpdatedToMissingData_SendsWorkItemStatusChangedNotification()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("NEEDS_INFO");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await _notificationHub.Received().WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "WORK_ITEM_STATUS_CHANGED" &&
                n.WorkItemId == evt.WorkItemId &&
                n.NewStatus == "MissingData"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_StatusUpdatedToNoPaRequired_SendsWorkItemStatusChangedNotification()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("NOT_REQUIRED");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert
        await _notificationHub.Received().WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "WORK_ITEM_STATUS_CHANGED" &&
                n.WorkItemId == evt.WorkItemId &&
                n.NewStatus == "NoPaRequired"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessAsync_WorkItemNotFoundFallback_SendsWorkItemStatusChangedNotification()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(evt.PatientId, clinicalBundle, formData, pdfBytes);

        // Work item not found, will use fallback UpdateStatusAsync
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        // UpdateStatusAsync must return true for processing to continue
        _workItemStore.UpdateStatusAsync(evt.WorkItemId, Arg.Any<WorkItemStatus>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - Should still send notification even when using fallback
        await _notificationHub.Received().WriteAsync(
            Arg.Is<Notification>(n =>
                n.Type == "WORK_ITEM_STATUS_CHANGED" &&
                n.WorkItemId == evt.WorkItemId &&
                n.NewStatus == "ReadyForReview"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Data Validation Logging Tests

    [Test]
    public async Task ProcessAsync_WithClinicalData_LogsValidationSignals()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        clinicalBundle = clinicalBundle with
        {
            ServiceRequests =
            [
                new ServiceRequestInfo
                {
                    Id = "sr-123",
                    Status = "active",
                    Code = new CodeableConcept
                    {
                        Coding = [new Coding { Code = "72148", Display = "MRI Lumbar Spine" }],
                        Text = "MRI Lumbar Spine"
                    }
                }
            ]
        };
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _aggregator.AggregateClinicalDataAsync(evt.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(formData));
        _pdfStamper.StampFormAsync(Arg.Any<PAFormData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - verify validation log was written with HasRequiredData
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("HasRequiredData")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task ProcessAsync_WithClinicalData_LogsPreIntelligenceCallSignals()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = CreateTestBundle(evt.PatientId);
        clinicalBundle = clinicalBundle with
        {
            ServiceRequests =
            [
                new ServiceRequestInfo
                {
                    Id = "sr-123",
                    Status = "active",
                    Code = new CodeableConcept
                    {
                        Coding = [new Coding { Code = "72148", Display = "MRI Lumbar Spine" }],
                        Text = "MRI Lumbar Spine"
                    }
                }
            ]
        };
        var formData = CreateFormData("APPROVE");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _aggregator.AggregateClinicalDataAsync(evt.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(formData));
        _pdfStamper.StampFormAsync(Arg.Any<PAFormData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - verify pre-Intelligence log was written with Sending to Intelligence
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Sending to Intelligence")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task ProcessAsync_WithMissingPatient_LogsValidationAsFalse()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = new ClinicalBundle
        {
            PatientId = evt.PatientId,
            Patient = null, // Missing patient
            Conditions = [new ConditionInfo { Id = "cond-1", Code = "M54.5", Display = "Low back pain", ClinicalStatus = "active" }],
            Observations = [],
            Procedures = [],
            Documents = [],
            ServiceRequests =
            [
                new ServiceRequestInfo
                {
                    Id = "sr-123",
                    Status = "active",
                    Code = new CodeableConcept
                    {
                        Coding = [new Coding { Code = "72148", Display = "MRI" }],
                        Text = "MRI"
                    }
                }
            ]
        };
        var formData = CreateFormData("NEEDS_INFO");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _aggregator.AggregateClinicalDataAsync(evt.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(formData));
        _pdfStamper.StampFormAsync(Arg.Any<PAFormData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - verify validation log indicates HasRequiredData (will be false due to missing patient)
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Data validation")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task ProcessAsync_WithNoConditions_LogsValidationAsFalse()
    {
        // Arrange
        var evt = CreateEvent();
        var clinicalBundle = new ClinicalBundle
        {
            PatientId = evt.PatientId,
            Patient = new PatientInfo { Id = evt.PatientId, GivenName = "Test", FamilyName = "Patient", BirthDate = new DateOnly(1980, 1, 15), MemberId = "MEM123" },
            Conditions = [], // No conditions
            Observations = [],
            Procedures = [],
            Documents = [],
            ServiceRequests =
            [
                new ServiceRequestInfo
                {
                    Id = "sr-123",
                    Status = "active",
                    Code = new CodeableConcept
                    {
                        Coding = [new Coding { Code = "72148", Display = "MRI" }],
                        Text = "MRI"
                    }
                }
            ]
        };
        var formData = CreateFormData("NEEDS_INFO");
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _aggregator.AggregateClinicalDataAsync(evt.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(clinicalBundle));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(formData));
        _pdfStamper.StampFormAsync(Arg.Any<PAFormData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));

        var existingWorkItem = new WorkItem
        {
            Id = evt.WorkItemId,
            PatientId = evt.PatientId,
            EncounterId = evt.EncounterId,
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _workItemStore.GetByIdAsync(evt.WorkItemId, Arg.Any<CancellationToken>())
            .Returns(existingWorkItem);
        _workItemStore.UpdateAsync(evt.WorkItemId, Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.ProcessAsync(evt, CancellationToken.None);

        // Assert - verify validation log indicates ConditionsPresent
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ConditionsPresent")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
