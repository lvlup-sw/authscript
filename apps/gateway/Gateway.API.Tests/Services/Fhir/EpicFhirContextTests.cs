namespace Gateway.API.Tests.Services.Fhir;

using System.Net;
using Gateway.API.Contracts;
using Gateway.API.Contracts.Fhir;
using Gateway.API.Services.Fhir;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Task = System.Threading.Tasks.Task;

public class EpicFhirContextTests
{
    private readonly IFhirSerializer _fhirSerializer;
    private readonly ILogger<EpicFhirContext<Patient>> _logger;

    public EpicFhirContextTests()
    {
        _fhirSerializer = Substitute.For<IFhirSerializer>();
        _logger = Substitute.For<ILogger<EpicFhirContext<Patient>>>();
    }

    [Test]
    public async Task ReadAsync_Success_UsesFhirSerializer()
    {
        // Arrange
        var patient = new Patient { Id = "123" };
        var patientJson = """{"resourceType":"Patient","id":"123"}""";

        var handler = new MockHttpMessageHandler(patientJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _fhirSerializer.Deserialize<Patient>(Arg.Any<string>()).Returns(patient);

        var context = new EpicFhirContext<Patient>(httpClient, _fhirSerializer, _logger);

        // Act
        var result = await context.ReadAsync("123", "token");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Id).IsEqualTo("123");
        _fhirSerializer.Received(1).Deserialize<Patient>(Arg.Any<string>());
    }

    [Test]
    public async Task SearchAsync_Success_UsesDeserializeBundle()
    {
        // Arrange
        var bundle = new Bundle
        {
            Entry = new List<Bundle.EntryComponent>
            {
                new() { Resource = new Patient { Id = "p1" } },
                new() { Resource = new Patient { Id = "p2" } }
            }
        };
        var bundleJson = """{"resourceType":"Bundle","entry":[]}""";

        var handler = new MockHttpMessageHandler(bundleJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _fhirSerializer.DeserializeBundle(Arg.Any<string>()).Returns(bundle);

        var context = new EpicFhirContext<Patient>(httpClient, _fhirSerializer, _logger);

        // Act
        var result = await context.SearchAsync("_id=123", "token");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
        _fhirSerializer.Received(1).DeserializeBundle(Arg.Any<string>());
    }

    [Test]
    public async Task ReadAsync_NotFound_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        var context = new EpicFhirContext<Patient>(httpClient, _fhirSerializer, _logger);

        var result = await context.ReadAsync("999", "token");

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Code).IsEqualTo("NOT_FOUND");
    }

    [Test]
    public async Task ReadAsync_Unauthorized_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        var context = new EpicFhirContext<Patient>(httpClient, _fhirSerializer, _logger);

        var result = await context.ReadAsync("123", "invalid-token");

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Code).IsEqualTo("UNAUTHORIZED");
    }

    [Test]
    public async Task ReadAsync_DeserializationFails_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler("""{"resourceType":"Patient"}""", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _fhirSerializer.Deserialize<Patient>(Arg.Any<string>()).Returns((Patient?)null);

        var context = new EpicFhirContext<Patient>(httpClient, _fhirSerializer, _logger);

        var result = await context.ReadAsync("123", "token");

        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error!.Code).IsEqualTo("VALIDATION_ERROR");
    }

    [Test]
    public async Task CreateAsync_Success_UsesFhirSerializer()
    {
        // Arrange
        var patient = new Patient { Id = "new-123" };
        var responseJson = """{"resourceType":"Patient","id":"new-123"}""";

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.Created);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://fhir.test/") };

        _fhirSerializer.Deserialize<Patient>(Arg.Any<string>()).Returns(patient);

        var context = new EpicFhirContext<Patient>(httpClient, _fhirSerializer, _logger);

        // Act
        var result = await context.CreateAsync(new Patient(), "token");

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Id).IsEqualTo("new-123");
        _fhirSerializer.Received(1).Deserialize<Patient>(Arg.Any<string>());
    }

    /// <summary>
    /// Helper class for mocking HttpClient.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
        {
            _response = response;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_response)
            });
        }
    }
}
