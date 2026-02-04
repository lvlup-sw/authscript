using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for the DocumentUploader service.
/// </summary>
public class DocumentUploaderTests
{
    private readonly IFhirHttpClient _fhirHttpClient;
    private readonly ILogger<DocumentUploader> _logger;
    private readonly IOptions<DocumentOptions> _options;
    private readonly DocumentUploader _sut;

    public DocumentUploaderTests()
    {
        _fhirHttpClient = Substitute.For<IFhirHttpClient>();
        _logger = Substitute.For<ILogger<DocumentUploader>>();
        _options = Options.Create(new DocumentOptions
        {
            PriorAuthLoincCode = "64290-0",
            PriorAuthLoincDisplay = "Prior Authorization"
        });
        _sut = new DocumentUploader(_fhirHttpClient, _logger, _options);
    }

    [Test]
    public async Task DocumentUploader_UploadDocumentAsync_CreatesDocumentReference()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        const string patientId = "patient-123";
        const string encounterId = "encounter-456";
                const string documentId = "doc-789";

        var responseJson = JsonDocument.Parse($"{{\"id\": \"{documentId}\"}}").RootElement;
        _fhirHttpClient.CreateAsync("DocumentReference", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(responseJson));

        // Act
        var result = await _sut.UploadDocumentAsync(pdfBytes, patientId, encounterId, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(documentId);
        await _fhirHttpClient.Received(1).CreateAsync(
            "DocumentReference",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DocumentUploader_UploadDocumentAsync_EncodesContentAsBase64()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        var expectedBase64 = Convert.ToBase64String(pdfBytes);
        const string patientId = "patient-123";
        
        string? capturedJson = null;
        var responseJson = JsonDocument.Parse("{\"id\": \"doc-123\"}").RootElement;
        _fhirHttpClient.CreateAsync("DocumentReference", Arg.Do<string>(json => capturedJson = json), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(responseJson));

        // Act
        await _sut.UploadDocumentAsync(pdfBytes, patientId, null, CancellationToken.None);

        // Assert
        await Assert.That(capturedJson).IsNotNull();
        await Assert.That(capturedJson!).Contains(expectedBase64);
    }

    [Test]
    public async Task DocumentUploader_UploadDocumentAsync_SetsCorrectContentType()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        const string patientId = "patient-123";
        
        string? capturedJson = null;
        var responseJson = JsonDocument.Parse("{\"id\": \"doc-123\"}").RootElement;
        _fhirHttpClient.CreateAsync("DocumentReference", Arg.Do<string>(json => capturedJson = json), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(responseJson));

        // Act
        await _sut.UploadDocumentAsync(pdfBytes, patientId, null, CancellationToken.None);

        // Assert
        await Assert.That(capturedJson).IsNotNull();
        await Assert.That(capturedJson!).Contains("application/pdf");
    }

    [Test]
    public async Task DocumentUploader_UploadDocumentAsync_LinksToPatientAndEncounter()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        const string patientId = "patient-123";
        const string encounterId = "encounter-456";
        
        string? capturedJson = null;
        var responseJson = JsonDocument.Parse("{\"id\": \"doc-123\"}").RootElement;
        _fhirHttpClient.CreateAsync("DocumentReference", Arg.Do<string>(json => capturedJson = json), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(responseJson));

        // Act
        await _sut.UploadDocumentAsync(pdfBytes, patientId, encounterId, CancellationToken.None);

        // Assert
        await Assert.That(capturedJson).IsNotNull();
        await Assert.That(capturedJson!).Contains($"Patient/{patientId}");
        await Assert.That(capturedJson!).Contains($"Encounter/{encounterId}");
    }

    [Test]
    public async Task DocumentUploader_UploadDocumentAsync_ReturnsFailure_WhenFhirFails()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        const string patientId = "patient-123";
        
        _fhirHttpClient.CreateAsync("DocumentReference", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(FhirError.Unauthorized("Invalid token")));

        // Act
        var result = await _sut.UploadDocumentAsync(pdfBytes, patientId, null, CancellationToken.None);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Code).IsEqualTo("UNAUTHORIZED");
    }

    [Test]
    public async Task DocumentUploader_UploadDocumentAsync_SetsLoincCode()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        const string patientId = "patient-123";
        
        string? capturedJson = null;
        var responseJson = JsonDocument.Parse("{\"id\": \"doc-123\"}").RootElement;
        _fhirHttpClient.CreateAsync("DocumentReference", Arg.Do<string>(json => capturedJson = json), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(responseJson));

        // Act
        await _sut.UploadDocumentAsync(pdfBytes, patientId, null, CancellationToken.None);

        // Assert
        await Assert.That(capturedJson).IsNotNull();
        await Assert.That(capturedJson!).Contains("http://loinc.org");
        await Assert.That(capturedJson!).Contains("64290-0");
    }
}
