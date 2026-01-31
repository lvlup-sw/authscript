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
    private readonly ITokenAcquisitionStrategy _tokenStrategy;
    private readonly ILogger<EncounterProcessor> _logger;
    private readonly EncounterProcessor _sut;

    public EncounterProcessorTests()
    {
        _aggregator = Substitute.For<IFhirDataAggregator>();
        _intelligenceClient = Substitute.For<IIntelligenceClient>();
        _pdfStamper = Substitute.For<IPdfFormStamper>();
        _resultStore = Substitute.For<IAnalysisResultStore>();
        _notificationHub = Substitute.For<INotificationHub>();
        _tokenStrategy = Substitute.For<ITokenAcquisitionStrategy>();
        _logger = Substitute.For<ILogger<EncounterProcessor>>();

        // Default token strategy behavior: returns a valid token
        _tokenStrategy.CanHandle.Returns(true);
        _tokenStrategy.AcquireTokenAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("test-access-token"));

        _sut = new EncounterProcessor(
            _aggregator,
            _intelligenceClient,
            _pdfStamper,
            _resultStore,
            _notificationHub,
            _tokenStrategy,
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
            Arg.Any<string>(),
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

        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string>(), Arg.Any<CancellationToken>())
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

        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string>(), Arg.Any<CancellationToken>())
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

        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string>(), Arg.Any<CancellationToken>())
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

        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string>(), Arg.Any<CancellationToken>())
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

    [Test]
    public async Task ProcessEncounterAsync_WhenTokenUnavailable_ReturnsEarlyWithoutProcessing()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        _tokenStrategy.CanHandle.Returns(true);
        _tokenStrategy.AcquireTokenAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - No further processing should occur
        await _aggregator.DidNotReceive().AggregateClinicalDataAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_WhenStrategyCannotHandle_ReturnsEarlyWithoutProcessing()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";

        _tokenStrategy.CanHandle.Returns(false);

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - No further processing should occur
        await _aggregator.DidNotReceive().AggregateClinicalDataAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        // Token acquisition should not even be attempted when CanHandle is false
        await _tokenStrategy.DidNotReceive().AcquireTokenAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessEncounterAsync_AcquiresTokenViaStrategy_PassesToAggregator()
    {
        // Arrange
        const string encounterId = "enc-123";
        const string patientId = "patient-456";
        const string expectedToken = "acquired-token-123";

        _tokenStrategy.CanHandle.Returns(true);
        _tokenStrategy.AcquireTokenAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(expectedToken));

        var clinicalBundle = CreateTestBundle(patientId);
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        SetupSuccessfulMocks(patientId, clinicalBundle, formData, pdfBytes);

        // Act
        await _sut.ProcessEncounterAsync(encounterId, patientId, CancellationToken.None);

        // Assert - Token should be passed to aggregator
        await _aggregator.Received(1).AggregateClinicalDataAsync(
            patientId,
            expectedToken,
            Arg.Any<CancellationToken>());
    }

    private void SetupSuccessfulMocks(
        string patientId,
        ClinicalBundle clinicalBundle,
        PAFormData formData,
        byte[] pdfBytes)
    {
        _aggregator.AggregateClinicalDataAsync(patientId, Arg.Any<string>(), Arg.Any<CancellationToken>())
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
}
