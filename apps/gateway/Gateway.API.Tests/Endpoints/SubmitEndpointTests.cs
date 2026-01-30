using Gateway.API.Contracts;
using Gateway.API.Endpoints;
using Gateway.API.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace Gateway.API.Tests.Endpoints;

/// <summary>
/// Tests for the SubmitEndpoints API endpoints.
/// </summary>
public class SubmitEndpointTests
{
    private readonly IAnalysisResultStore _resultStore;
    private readonly IDocumentUploader _documentUploader;
    private readonly IPdfFormStamper _pdfStamper;

    public SubmitEndpointTests()
    {
        _resultStore = Substitute.For<IAnalysisResultStore>();
        _documentUploader = Substitute.For<IDocumentUploader>();
        _pdfStamper = Substitute.For<IPdfFormStamper>();
    }

    [Test]
    public async Task SubmitEndpoint_Post_FetchesPdfFromResultStore()
    {
        // Arrange
        const string transactionId = "txn-123";
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var request = CreateSubmitRequest();

        _resultStore.GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(pdfBytes);

        _documentUploader.UploadDocumentAsync(
                Arg.Any<byte[]>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("doc-456"));

        // Act
        var result = await InvokeSubmit(transactionId, request);

        // Assert
        await _resultStore.Received(1).GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitEndpoint_Post_CallsDocumentUploader()
    {
        // Arrange
        const string transactionId = "txn-123";
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var request = CreateSubmitRequest();

        _resultStore.GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(pdfBytes);

        _documentUploader.UploadDocumentAsync(
                Arg.Any<byte[]>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("doc-456"));

        // Act
        await InvokeSubmit(transactionId, request);

        // Assert
        await _documentUploader.Received(1).UploadDocumentAsync(
            pdfBytes,
            request.PatientId,
            request.EncounterId,
            request.AccessToken,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitEndpoint_Post_Returns404WhenResultNotFound()
    {
        // Arrange
        const string transactionId = "txn-nonexistent";
        var request = CreateSubmitRequest();

        _resultStore.GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _resultStore.GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((PAFormData?)null);

        // Act
        var result = await InvokeSubmit(transactionId, request);

        // Assert
        await Assert.That(result).IsNotNull();
        var notFoundResult = result as NotFound<ErrorResponse>;
        await Assert.That(notFoundResult).IsNotNull();
        await Assert.That(notFoundResult!.Value!.Message).Contains("not found");
    }

    [Test]
    public async Task SubmitEndpoint_Post_Returns200OnSuccess()
    {
        // Arrange
        const string transactionId = "txn-123";
        const string documentId = "doc-uploaded-789";
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var request = CreateSubmitRequest();

        _resultStore.GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(pdfBytes);

        _documentUploader.UploadDocumentAsync(
                Arg.Any<byte[]>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(documentId));

        // Act
        var result = await InvokeSubmit(transactionId, request);

        // Assert
        await Assert.That(result).IsNotNull();
        var okResult = result as Ok<SubmitResponse>;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(okResult!.Value).IsNotNull();
        await Assert.That(okResult.Value!.Submitted).IsTrue();
        await Assert.That(okResult.Value.DocumentId).IsEqualTo(documentId);
    }

    [Test]
    public async Task SubmitEndpoint_Post_GeneratesPdfWhenNotCached()
    {
        // Arrange
        const string transactionId = "txn-123";
        var formData = CreateTestFormData();
        var generatedPdf = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var request = CreateSubmitRequest();

        _resultStore.GetCachedPdfAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        _resultStore.GetCachedResponseAsync(transactionId, Arg.Any<CancellationToken>())
            .Returns(formData);

        _pdfStamper.StampFormAsync(formData, Arg.Any<CancellationToken>())
            .Returns(generatedPdf);

        _documentUploader.UploadDocumentAsync(
                Arg.Any<byte[]>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("doc-456"));

        // Act
        await InvokeSubmit(transactionId, request);

        // Assert
        await _pdfStamper.Received(1).StampFormAsync(formData, Arg.Any<CancellationToken>());
    }

    private static SubmitRequest CreateSubmitRequest()
    {
        return new SubmitRequest
        {
            PatientId = "patient-123",
            EncounterId = "encounter-456",
            AccessToken = "bearer-token"
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
            ClinicalSummary = "Test summary",
            SupportingEvidence = [],
            Recommendation = "approve",
            ConfidenceScore = 0.92,
            FieldMappings = new Dictionary<string, string>()
        };
    }

    private async Task<Microsoft.AspNetCore.Http.IResult> InvokeSubmit(
        string transactionId,
        SubmitRequest request)
    {
        return await SubmitEndpoints.SubmitAsync(
            transactionId,
            request,
            _documentUploader,
            _resultStore,
            _pdfStamper,
            CancellationToken.None);
    }
}
