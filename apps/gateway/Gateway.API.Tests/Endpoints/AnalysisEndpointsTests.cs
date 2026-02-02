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
    private readonly IAnalysisResultStore _resultStore;
    private readonly IPdfFormStamper _pdfStamper;
    private readonly IDocumentUploader _documentUploader;

    public AnalysisEndpointsTests()
    {
        _resultStore = Substitute.For<IAnalysisResultStore>();
        _pdfStamper = Substitute.For<IPdfFormStamper>();
        _documentUploader = Substitute.For<IDocumentUploader>();
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

        _resultStore
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

        _resultStore
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
            _resultStore,
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

        _resultStore
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

        _resultStore
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
            _resultStore,
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

        _resultStore
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

        _resultStore
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _resultStore
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
        await _resultStore.Received(1).SetCachedPdfAsync(
            transactionId,
            generatedPdfBytes,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DownloadForm_WhenNoAnalysisData_Returns404()
    {
        // Arrange
        const string transactionId = "txn-nonexistent";

        _resultStore
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _resultStore
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
            _resultStore,
            _pdfStamper,
            CancellationToken.None);
    }

    #endregion

    #region SubmitToFhir Tests

    [Test]
    public async Task SubmitToFhir_WhenAnalysisExists_CallsUploaderAndReturnsSuccess()
    {
        // Arrange
        const string transactionId = "txn-12345";
        const string documentId = "doc-uploaded-123";
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var request = new SubmitToFhirRequest
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456"
        };

        _resultStore
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(formData);

        _resultStore
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(pdfBytes);

        _documentUploader
            .UploadDocumentAsync(
                pdfBytes,
                request.PatientId,
                request.EncounterId,
                Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(documentId));

        // Act
        var result = await InvokeSubmitToFhir(transactionId, request);

        // Assert
        await Assert.That(result).IsNotNull();
        var okResult = result as Ok<SubmitResponse>;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(okResult!.Value).IsNotNull();
        await Assert.That(okResult.Value!.Submitted).IsTrue();
        await Assert.That(okResult.Value.DocumentId).IsEqualTo(documentId);

        // Verify uploader was called
        await _documentUploader.Received(1).UploadDocumentAsync(
            pdfBytes,
            request.PatientId,
            request.EncounterId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitToFhir_WhenNoPdfAvailable_Returns404()
    {
        // Arrange
        const string transactionId = "txn-nonexistent";
        var request = new SubmitToFhirRequest
        {
            PatientId = "patient-123"
        };

        _resultStore
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _resultStore
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((PAFormData?)null);

        // Act
        var result = await InvokeSubmitToFhir(transactionId, request);

        // Assert
        await Assert.That(result).IsNotNull();
        var notFoundResult = result as NotFound<ErrorResponse>;
        await Assert.That(notFoundResult).IsNotNull();
    }

    [Test]
    public async Task SubmitToFhir_WhenUploadFails_ReturnsError()
    {
        // Arrange
        const string transactionId = "txn-12345";
        var formData = CreateTestFormData();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var request = new SubmitToFhirRequest
        {
            PatientId = "patient-123"
        };

        _resultStore
            .GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(formData);

        _resultStore
            .GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(pdfBytes);

        _documentUploader
            .UploadDocumentAsync(
                Arg.Any<byte[]>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(FhirError.Unauthorized("FHIR returned 401")));

        // Act
        var result = await InvokeSubmitToFhir(transactionId, request);

        // Assert
        await Assert.That(result).IsNotNull();
        var problemResult = result as ProblemHttpResult;
        await Assert.That(problemResult).IsNotNull();
    }

    private async Task<IResult> InvokeSubmitToFhir(string transactionId, SubmitToFhirRequest request)
    {
        return await Gateway.API.Endpoints.AnalysisEndpoints.SubmitToFhirAsync(
            transactionId,
            request,
            _documentUploader,
            _resultStore,
            _pdfStamper,
            CancellationToken.None);
    }

    #endregion
}
