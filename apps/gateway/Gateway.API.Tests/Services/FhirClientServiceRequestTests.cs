using System.Text.Json;
using Gateway.API.Contracts;
using Gateway.API.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for FhirClient.SearchServiceRequestsAsync method.
/// </summary>
public class FhirClientServiceRequestTests
{
    private readonly IFhirHttpClient _httpClient;
    private readonly ILogger<FhirClient> _logger;
    private readonly FhirClient _sut;

    public FhirClientServiceRequestTests()
    {
        _httpClient = Substitute.For<IFhirHttpClient>();
        _logger = Substitute.For<ILogger<FhirClient>>();
        _sut = new FhirClient(_httpClient, _logger);
    }

    [Test]
    public async Task SearchServiceRequestsAsync_ValidBundle_ExtractsServiceRequests()
    {
        // Arrange
        const string fhirBundle = """
            {
                "resourceType": "Bundle",
                "type": "searchset",
                "entry": [
                    {
                        "resource": {
                            "resourceType": "ServiceRequest",
                            "id": "sr-123",
                            "status": "active",
                            "code": {
                                "coding": [
                                    {
                                        "system": "http://www.ama-assn.org/go/cpt",
                                        "code": "70553",
                                        "display": "MRI brain with contrast"
                                    }
                                ]
                            },
                            "encounter": {
                                "reference": "Encounter/enc-456"
                            },
                            "authoredOn": "2025-01-15T10:30:00Z"
                        }
                    },
                    {
                        "resource": {
                            "resourceType": "ServiceRequest",
                            "id": "sr-789",
                            "status": "draft",
                            "code": {
                                "coding": [
                                    {
                                        "system": "http://www.ama-assn.org/go/cpt",
                                        "code": "27447",
                                        "display": "Total knee replacement"
                                    }
                                ]
                            }
                        }
                    }
                ]
            }
            """;

        var jsonDocument = JsonDocument.Parse(fhirBundle);
        _httpClient.SearchAsync("ServiceRequest", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(jsonDocument.RootElement));

        // Act
        var serviceRequests = await _sut.SearchServiceRequestsAsync("patient-1", null, "token", CancellationToken.None);

        // Assert
        await Assert.That(serviceRequests.Count).IsEqualTo(2);
        await Assert.That(serviceRequests[0].Id).IsEqualTo("sr-123");
        await Assert.That(serviceRequests[0].Status).IsEqualTo("active");
        await Assert.That(serviceRequests[0].EncounterId).IsEqualTo("enc-456");
        await Assert.That(serviceRequests[1].Id).IsEqualTo("sr-789");
        await Assert.That(serviceRequests[1].Status).IsEqualTo("draft");
        await Assert.That(serviceRequests[1].EncounterId).IsNull();
    }

    [Test]
    public async Task SearchServiceRequestsAsync_ExtractsCptCode_FromCodeableConcept()
    {
        // Arrange
        const string fhirBundle = """
            {
                "resourceType": "Bundle",
                "type": "searchset",
                "entry": [
                    {
                        "resource": {
                            "resourceType": "ServiceRequest",
                            "id": "sr-456",
                            "status": "active",
                            "code": {
                                "coding": [
                                    {
                                        "system": "http://www.ama-assn.org/go/cpt",
                                        "code": "70553",
                                        "display": "MRI brain with contrast"
                                    }
                                ],
                                "text": "Brain MRI with gadolinium"
                            }
                        }
                    }
                ]
            }
            """;

        var jsonDocument = JsonDocument.Parse(fhirBundle);
        _httpClient.SearchAsync("ServiceRequest", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(jsonDocument.RootElement));

        // Act
        var serviceRequests = await _sut.SearchServiceRequestsAsync("patient-1", null, "token", CancellationToken.None);

        // Assert
        await Assert.That(serviceRequests.Count).IsEqualTo(1);
        var code = serviceRequests[0].Code;
        await Assert.That(code).IsNotNull();
        await Assert.That(code.Coding).IsNotNull();
        await Assert.That(code.Coding!.Count).IsEqualTo(1);
        await Assert.That(code.Coding[0].System).IsEqualTo("http://www.ama-assn.org/go/cpt");
        await Assert.That(code.Coding[0].Code).IsEqualTo("70553");
        await Assert.That(code.Coding[0].Display).IsEqualTo("MRI brain with contrast");
        await Assert.That(code.Text).IsEqualTo("Brain MRI with gadolinium");
    }

    [Test]
    public async Task SearchServiceRequestsAsync_FiltersByEncounter_WhenProvided()
    {
        // Arrange
        const string fhirBundle = """
            {
                "resourceType": "Bundle",
                "type": "searchset",
                "entry": []
            }
            """;

        var jsonDocument = JsonDocument.Parse(fhirBundle);
        _httpClient.SearchAsync("ServiceRequest", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(jsonDocument.RootElement));

        // Act
        await _sut.SearchServiceRequestsAsync("patient-1", "enc-789", "token", CancellationToken.None);

        // Assert - verify the query includes encounter filter
        await _httpClient.Received(1).SearchAsync(
            "ServiceRequest",
            "patient=patient-1&encounter=enc-789",
            "token",
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchServiceRequestsAsync_OnFailure_ReturnsEmptyList()
    {
        // Arrange
        _httpClient.SearchAsync("ServiceRequest", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(FhirError.Network("Connection failed")));

        // Act
        var serviceRequests = await _sut.SearchServiceRequestsAsync("patient-1", null, "token", CancellationToken.None);

        // Assert
        await Assert.That(serviceRequests).IsEmpty();
    }

    [Test]
    public async Task SearchServiceRequestsAsync_ParsesAuthoredOn_Correctly()
    {
        // Arrange
        const string fhirBundle = """
            {
                "resourceType": "Bundle",
                "type": "searchset",
                "entry": [
                    {
                        "resource": {
                            "resourceType": "ServiceRequest",
                            "id": "sr-100",
                            "status": "active",
                            "code": {
                                "coding": [
                                    {
                                        "system": "http://www.ama-assn.org/go/cpt",
                                        "code": "99213"
                                    }
                                ]
                            },
                            "authoredOn": "2025-01-20T14:45:30Z"
                        }
                    }
                ]
            }
            """;

        var jsonDocument = JsonDocument.Parse(fhirBundle);
        _httpClient.SearchAsync("ServiceRequest", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(jsonDocument.RootElement));

        // Act
        var serviceRequests = await _sut.SearchServiceRequestsAsync("patient-1", null, "token", CancellationToken.None);

        // Assert
        await Assert.That(serviceRequests.Count).IsEqualTo(1);
        await Assert.That(serviceRequests[0].AuthoredOn).IsNotNull();
        await Assert.That(serviceRequests[0].AuthoredOn!.Value.Year).IsEqualTo(2025);
        await Assert.That(serviceRequests[0].AuthoredOn!.Value.Month).IsEqualTo(1);
        await Assert.That(serviceRequests[0].AuthoredOn!.Value.Day).IsEqualTo(20);
    }
}
