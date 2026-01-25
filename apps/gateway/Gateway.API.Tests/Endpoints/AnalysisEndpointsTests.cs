using Gateway.API.Contracts;
using Gateway.API.Models;
using Gateway.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gateway.API.Tests.Endpoints;

/// <summary>
/// Tests for the AnalysisEndpoints API endpoints.
/// </summary>
public class AnalysisEndpointsTests
{
    private readonly IDemoCacheService _cacheService;
    private readonly IPdfFormStamper _pdfStamper;
    private readonly IEpicUploader _epicUploader;

    public AnalysisEndpointsTests()
    {
        _cacheService = Substitute.For<IDemoCacheService>();
        _pdfStamper = Substitute.For<IPdfFormStamper>();
        _epicUploader = Substitute.For<IEpicUploader>();
    }

    private static PAFormData CreateTestFormData(string patientName = "John Doe")
    {
        return new PAFormData
        {
            PatientName = patientName,
            PatientDob = "1985-03-15",
            MemberId = "MEM123456",
            DiagnosisCodes = ["M54.5", "M47.816"],
            ProcedureCode = "72148",
            ClinicalSummary = "Patient presents with chronic low back pain...",
            SupportingEvidence =
            [
                new EvidenceItem
                {
                    CriterionId = "clinical_indication",
                    Status = "met",
                    Evidence = "Chronic low back pain with radiculopathy",
                    Source = "Progress Note",
                    Confidence = 0.95
                }
            ],
            Recommendation = "approve",
            ConfidenceScore = 0.92,
            FieldMappings = new Dictionary<string, string>
            {
                ["patient_name"] = patientName,
                ["dob"] = "1985-03-15"
            }
        };
    }

    #region GetAnalysis Tests

    [Test]
    public async Task GetAnalysis_WhenAnalysisExists_ReturnsAnalysisData()
    {
        // Arrange
        const string transactionId = "txn-12345";
        var expectedFormData = CreateTestFormData();

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(expectedFormData);

        // Act
        var result = await InvokeGetAnalysis(transactionId);

        // Assert
        await Assert.That(result).IsNotNull();
        var okResult = result as Ok<AnalysisResponse>;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(okResult!.Value).IsNotNull();
        await Assert.That(okResult.Value!.TransactionId).IsEqualTo(transactionId);
        await Assert.That(okResult.Value.Status).IsEqualTo("completed");
        await Assert.That(okResult.Value.FormData).IsNotNull();
        await Assert.That(okResult.Value.FormData!.PatientName).IsEqualTo("John Doe");
    }

    [Test]
    public async Task GetAnalysis_WhenNotFound_Returns404()
    {
        // Arrange
        const string transactionId = "txn-nonexistent";

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((PAFormData?)null);

        // Act
        var result = await InvokeGetAnalysis(transactionId);

        // Assert
        await Assert.That(result).IsNotNull();
        var notFoundResult = result as NotFound<ErrorResponse>;
        await Assert.That(notFoundResult).IsNotNull();
        await Assert.That(notFoundResult!.Value).IsNotNull();
        await Assert.That(notFoundResult.Value!.Message).Contains("not found");
    }

    private async Task<IResult> InvokeGetAnalysis(string transactionId)
    {
        return await Gateway.API.Endpoints.AnalysisEndpoints.GetAnalysisAsync(
            transactionId,
            _cacheService,
            CancellationToken.None);
    }

    #endregion

    #region GetStatus Tests

    [Test]
    public async Task GetStatus_WhenAnalysisComplete_ReturnsCompletedStatus()
    {
        // Arrange
        const string transactionId = "txn-12345";
        var formData = CreateTestFormData();

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(formData);

        // Act
        var result = await InvokeGetStatus(transactionId);

        // Assert
        await Assert.That(result).IsNotNull();
        var okResult = result as Ok<StatusResponse>;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(okResult!.Value).IsNotNull();
        await Assert.That(okResult.Value!.TransactionId).IsEqualTo(transactionId);
        await Assert.That(okResult.Value.Step).IsEqualTo("completed");
        await Assert.That(okResult.Value.Progress).IsEqualTo(100);
    }

    [Test]
    public async Task GetStatus_WhenNotInCache_ReturnsInProgressStatus()
    {
        // Arrange
        const string transactionId = "txn-pending";

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((PAFormData?)null);

        // Act
        var result = await InvokeGetStatus(transactionId);

        // Assert
        await Assert.That(result).IsNotNull();
        var okResult = result as Ok<StatusResponse>;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(okResult!.Value).IsNotNull();
        await Assert.That(okResult.Value!.Step).IsEqualTo("in_progress");
        await Assert.That(okResult.Value.Progress).IsLessThan(100);
    }

    private async Task<IResult> InvokeGetStatus(string transactionId)
    {
        return await Gateway.API.Endpoints.AnalysisEndpoints.GetAnalysisStatusAsync(
            transactionId,
            _cacheService,
            CancellationToken.None);
    }

    #endregion

    #region DownloadForm Tests

    [Test]
    public async Task DownloadForm_WhenPdfCached_ReturnsCachedPdf()
    {
        // Arrange
        const string transactionId = "txn-12345";
        var expectedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes

        _cacheService
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(expectedPdfBytes);

        // Act
        var result = await InvokeDownloadForm(transactionId);

        // Assert
        await Assert.That(result).IsNotNull();
        var fileResult = result as FileContentHttpResult;
        await Assert.That(fileResult).IsNotNull();
        await Assert.That(fileResult!.ContentType).IsEqualTo("application/pdf");
    }

    [Test]
    public async Task DownloadForm_WhenPdfNotCachedButFormDataExists_GeneratesAndCachesPdf()
    {
        // Arrange
        const string transactionId = "txn-12345";
        var formData = CreateTestFormData();
        var generatedPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // PDF magic bytes

        _cacheService
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(formData);

        _pdfStamper
            .StampFormAsync(formData, Arg.Any<CancellationToken>())
            .Returns(generatedPdfBytes);

        // Act
        var result = await InvokeDownloadForm(transactionId);

        // Assert
        await Assert.That(result).IsNotNull();
        var fileResult = result as FileContentHttpResult;
        await Assert.That(fileResult).IsNotNull();

        // Verify PDF was cached
        await _cacheService.Received(1).SetCachedPdfAsync(
            transactionId,
            generatedPdfBytes,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DownloadForm_WhenNoAnalysisData_Returns404()
    {
        // Arrange
        const string transactionId = "txn-nonexistent";

        _cacheService
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((PAFormData?)null);

        // Act
        var result = await InvokeDownloadForm(transactionId);

        // Assert
        await Assert.That(result).IsNotNull();
        var notFoundResult = result as NotFound<ErrorResponse>;
        await Assert.That(notFoundResult).IsNotNull();
        await Assert.That(notFoundResult!.Value!.Message).Contains("found");
    }

    private async Task<IResult> InvokeDownloadForm(string transactionId)
    {
        return await Gateway.API.Endpoints.AnalysisEndpoints.DownloadFormAsync(
            transactionId,
            _cacheService,
            _pdfStamper,
            CancellationToken.None);
    }

    #endregion

    #region SubmitToEpic Tests

    [Test]
    public async Task SubmitToEpic_WhenAnalysisExists_CallsUploaderAndReturnsSuccess()
    {
        // Arrange
        const string transactionId = "txn-12345";
        const string documentId = "doc-uploaded-123";
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var request = new SubmitToEpicRequest
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            AccessToken = "bearer-token-xyz"
        };

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(formData);

        _cacheService
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(pdfBytes);

        _epicUploader
            .UploadDocumentAsync(
                pdfBytes,
                request.PatientId,
                request.EncounterId,
                request.AccessToken,
                Arg.Any<CancellationToken>())
            .Returns(documentId);

        // Act
        var result = await InvokeSubmitToEpic(transactionId, request);

        // Assert
        await Assert.That(result).IsNotNull();
        var okResult = result as Ok<SubmitResponse>;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(okResult!.Value).IsNotNull();
        await Assert.That(okResult.Value!.Submitted).IsTrue();
        await Assert.That(okResult.Value.DocumentId).IsEqualTo(documentId);

        // Verify uploader was called
        await _epicUploader.Received(1).UploadDocumentAsync(
            pdfBytes,
            request.PatientId,
            request.EncounterId,
            request.AccessToken,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitToEpic_WhenNoPdfAvailable_Returns404()
    {
        // Arrange
        const string transactionId = "txn-nonexistent";
        var request = new SubmitToEpicRequest
        {
            PatientId = "patient-123",
            AccessToken = "bearer-token-xyz"
        };

        _cacheService
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((PAFormData?)null);

        // Act
        var result = await InvokeSubmitToEpic(transactionId, request);

        // Assert
        await Assert.That(result).IsNotNull();
        var notFoundResult = result as NotFound<ErrorResponse>;
        await Assert.That(notFoundResult).IsNotNull();
    }

    [Test]
    public async Task SubmitToEpic_WhenUploadFails_ReturnsError()
    {
        // Arrange
        const string transactionId = "txn-12345";
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var request = new SubmitToEpicRequest
        {
            PatientId = "patient-123",
            AccessToken = "bearer-token-xyz"
        };

        _cacheService
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(formData);

        _cacheService
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(pdfBytes);

        _epicUploader
            .UploadDocumentAsync(
                Arg.Any<byte[]>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Epic returned 401"));

        // Act
        var result = await InvokeSubmitToEpic(transactionId, request);

        // Assert
        await Assert.That(result).IsNotNull();
        var problemResult = result as ProblemHttpResult;
        await Assert.That(problemResult).IsNotNull();
    }

    private async Task<IResult> InvokeSubmitToEpic(string transactionId, SubmitToEpicRequest request)
    {
        return await Gateway.API.Endpoints.AnalysisEndpoints.SubmitToEpicAsync(
            transactionId,
            request,
            _epicUploader,
            _cacheService,
            _pdfStamper,
            CancellationToken.None);
    }

    #endregion
}
