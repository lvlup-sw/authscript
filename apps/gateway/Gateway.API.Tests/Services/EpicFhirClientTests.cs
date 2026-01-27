namespace Gateway.API.Tests.Services;

using System.Net;
using Gateway.API.Abstractions;
using Gateway.API.Contracts;
using Gateway.API.Contracts.Fhir;
using Gateway.API.Contracts.Http;
using Gateway.API.Models;
using Gateway.API.Services;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Task = System.Threading.Tasks.Task;
using FhirCodeableConcept = Hl7.Fhir.Model.CodeableConcept;

/// <summary>
/// Tests for EpicFhirClient with Result pattern.
/// </summary>
public class EpicFhirClientTests
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly IFhirSerializer _fhirSerializer;
    private readonly ILogger<EpicFhirClient> _logger;

    public EpicFhirClientTests()
    {
        _httpClientProvider = Substitute.For<IHttpClientProvider>();
        _fhirSerializer = Substitute.For<IFhirSerializer>();
        _logger = Substitute.For<ILogger<EpicFhirClient>>();
    }

    #region GetPatientAsync Tests

    [Test]
    public async Task GetPatientAsync_Success_ReturnsPatientInfo()
    {
        // Arrange
        var patientJson = """{"resourceType":"Patient","id":"123","name":[{"family":"Doe","given":["John"]}]}""";
        var patient = new Patient
        {
            Id = "123",
            Name = { new HumanName { Family = "Doe", Given = new[] { "John" } } }
        };

        var handler = new MockHttpMessageHandler(patientJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);
        _fhirSerializer.Deserialize<Patient>(Arg.Any<string>()).Returns(patient);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.GetPatientAsync("123");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Id).IsEqualTo("123");
        await Assert.That(result.Value.FamilyName).IsEqualTo("Doe");
        await Assert.That(result.Value.GivenName).IsEqualTo("John");
    }

    [Test]
    public async Task GetPatientAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.GetPatientAsync("999");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task GetPatientAsync_AuthFails_ReturnsFailure()
    {
        // Arrange
        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns((HttpClient?)null);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.GetPatientAsync("123");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Type).IsEqualTo(ErrorType.Unauthorized);
    }

    [Test]
    public async Task GetPatientAsync_ServerError_ReturnsInfrastructureError()
    {
        // Arrange
        var handler = new MockHttpMessageHandler("Internal Server Error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.GetPatientAsync("123");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Type).IsEqualTo(ErrorType.Infrastructure);
    }

    #endregion

    #region SearchConditionsAsync Tests

    [Test]
    public async Task SearchConditionsAsync_Success_ReturnsConditions()
    {
        // Arrange
        var bundle = new Bundle
        {
            Entry = new List<Bundle.EntryComponent>
            {
                new()
                {
                    Resource = new Condition
                    {
                        Id = "c1",
                        Code = new FhirCodeableConcept("http://snomed.info/sct", "12345", "Test Condition")
                    }
                }
            }
        };
        var bundleJson = """{"resourceType":"Bundle","entry":[]}""";

        var handler = new MockHttpMessageHandler(bundleJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);
        _fhirSerializer.DeserializeBundle(Arg.Any<string>()).Returns(bundle);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.SearchConditionsAsync("patient-123");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        await Assert.That(result.Value![0].Code).IsEqualTo("12345");
    }

    [Test]
    public async Task SearchConditionsAsync_AuthFails_ReturnsUnauthorized()
    {
        // Arrange
        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns((HttpClient?)null);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.SearchConditionsAsync("patient-123");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Type).IsEqualTo(ErrorType.Unauthorized);
    }

    [Test]
    public async Task SearchConditionsAsync_EmptyBundle_ReturnsEmptyList()
    {
        // Arrange
        var bundle = new Bundle { Entry = new List<Bundle.EntryComponent>() };
        var bundleJson = """{"resourceType":"Bundle","entry":[]}""";

        var handler = new MockHttpMessageHandler(bundleJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);
        _fhirSerializer.DeserializeBundle(Arg.Any<string>()).Returns(bundle);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.SearchConditionsAsync("patient-123");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }

    #endregion

    #region SearchObservationsAsync Tests

    [Test]
    public async Task SearchObservationsAsync_Success_ReturnsObservations()
    {
        // Arrange
        var bundle = new Bundle
        {
            Entry = new List<Bundle.EntryComponent>
            {
                new()
                {
                    Resource = new Observation
                    {
                        Id = "obs1",
                        Code = new FhirCodeableConcept("http://loinc.org", "2093-3", "Cholesterol"),
                        Value = new Quantity { Value = 200, Unit = "mg/dL" }
                    }
                }
            }
        };
        var bundleJson = """{"resourceType":"Bundle","entry":[]}""";

        var handler = new MockHttpMessageHandler(bundleJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);
        _fhirSerializer.DeserializeBundle(Arg.Any<string>()).Returns(bundle);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.SearchObservationsAsync("patient-123", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)));

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        await Assert.That(result.Value![0].Code).IsEqualTo("2093-3");
    }

    #endregion

    #region SearchProceduresAsync Tests

    [Test]
    public async Task SearchProceduresAsync_Success_ReturnsProcedures()
    {
        // Arrange
        var bundle = new Bundle
        {
            Entry = new List<Bundle.EntryComponent>
            {
                new()
                {
                    Resource = new Procedure
                    {
                        Id = "proc1",
                        Code = new FhirCodeableConcept("http://www.ama-assn.org/go/cpt", "72148", "MRI Lumbar Spine"),
                        Status = EventStatus.Completed
                    }
                }
            }
        };
        var bundleJson = """{"resourceType":"Bundle","entry":[]}""";

        var handler = new MockHttpMessageHandler(bundleJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);
        _fhirSerializer.DeserializeBundle(Arg.Any<string>()).Returns(bundle);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.SearchProceduresAsync("patient-123", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)));

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        await Assert.That(result.Value![0].Code).IsEqualTo("72148");
    }

    #endregion

    #region SearchDocumentsAsync Tests

    [Test]
    public async Task SearchDocumentsAsync_Success_ReturnsDocuments()
    {
        // Arrange
        var docRef = new DocumentReference
        {
            Id = "doc1",
            Type = new FhirCodeableConcept("http://loinc.org", "34108-1", "Outpatient Note"),
            Content = new List<DocumentReference.ContentComponent>
            {
                new()
                {
                    Attachment = new Attachment
                    {
                        ContentType = "application/pdf",
                        Title = "Progress Note"
                    }
                }
            }
        };
        var bundle = new Bundle
        {
            Entry = new List<Bundle.EntryComponent> { new() { Resource = docRef } }
        };
        var bundleJson = """{"resourceType":"Bundle","entry":[]}""";

        var handler = new MockHttpMessageHandler(bundleJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);
        _fhirSerializer.DeserializeBundle(Arg.Any<string>()).Returns(bundle);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.SearchDocumentsAsync("patient-123");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(1);
        await Assert.That(result.Value![0].ContentType).IsEqualTo("application/pdf");
    }

    #endregion

    #region GetDocumentContentAsync Tests

    [Test]
    public async Task GetDocumentContentAsync_Success_ReturnsBytes()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        var handler = new MockHttpMessageHandler(pdfBytes);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.GetDocumentContentAsync("doc-123");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Length).IsEqualTo(4);
    }

    [Test]
    public async Task GetDocumentContentAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _httpClientProvider.GetAuthenticatedClientAsync("EpicFhir", Arg.Any<CancellationToken>())
            .Returns(httpClient);

        var client = new EpicFhirClient(_httpClientProvider, _fhirSerializer, _logger);

        // Act
        var result = await client.GetDocumentContentAsync("doc-missing");

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Type).IsEqualTo(ErrorType.NotFound);
    }

    #endregion

    #region Helper Classes

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, (HttpContent, HttpStatusCode)> _responseFactory;

        public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
        {
            _responseFactory = _ => (new StringContent(response), statusCode);
        }

        public MockHttpMessageHandler(byte[] bytes)
        {
            _responseFactory = _ => (new ByteArrayContent(bytes), HttpStatusCode.OK);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var (content, statusCode) = _responseFactory(request);
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = content });
        }
    }

    #endregion
}
