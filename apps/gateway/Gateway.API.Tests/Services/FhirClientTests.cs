using System.Text.Json;
using Gateway.API.Contracts;
using Gateway.API.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for FhirClient JSON extraction methods.
/// </summary>
public class FhirClientTests
{
    private readonly IFhirHttpClient _httpClient;
    private readonly ILogger<FhirClient> _logger;
    private readonly FhirClient _sut;

    public FhirClientTests()
    {
        _httpClient = Substitute.For<IFhirHttpClient>();
        _logger = Substitute.For<ILogger<FhirClient>>();
        _sut = new FhirClient(_httpClient, _logger);
    }

    [Test]
    public async Task SearchConditionsAsync_ExtractsClinicalStatus_FromCodeableConcept()
    {
        // Arrange
        const string fhirBundle = """
            {
                "resourceType": "Bundle",
                "type": "searchset",
                "entry": [
                    {
                        "resource": {
                            "resourceType": "Condition",
                            "id": "cond-123",
                            "clinicalStatus": {
                                "coding": [
                                    {
                                        "system": "http://terminology.hl7.org/CodeSystem/condition-clinical",
                                        "code": "active",
                                        "display": "Active"
                                    }
                                ]
                            },
                            "code": {
                                "coding": [
                                    {
                                        "system": "http://hl7.org/fhir/sid/icd-10-cm",
                                        "code": "E11.9",
                                        "display": "Type 2 diabetes mellitus without complications"
                                    }
                                ]
                            }
                        }
                    }
                ]
            }
            """;

        var jsonDocument = JsonDocument.Parse(fhirBundle);
        _httpClient.SearchAsync("Condition", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(jsonDocument.RootElement));

        // Act
        var conditions = await _sut.SearchConditionsAsync("patient-1", "token", CancellationToken.None);

        // Assert
        await Assert.That(conditions.Count).IsEqualTo(1);
        await Assert.That(conditions[0].ClinicalStatus).IsEqualTo("active");
        await Assert.That(conditions[0].Code).IsEqualTo("E11.9");
    }

    [Test]
    public async Task SearchConditionsAsync_ReturnsNullClinicalStatus_WhenMissing()
    {
        // Arrange
        const string fhirBundle = """
            {
                "resourceType": "Bundle",
                "type": "searchset",
                "entry": [
                    {
                        "resource": {
                            "resourceType": "Condition",
                            "id": "cond-456",
                            "code": {
                                "coding": [
                                    {
                                        "system": "http://hl7.org/fhir/sid/icd-10-cm",
                                        "code": "J06.9",
                                        "display": "Acute upper respiratory infection"
                                    }
                                ]
                            }
                        }
                    }
                ]
            }
            """;

        var jsonDocument = JsonDocument.Parse(fhirBundle);
        _httpClient.SearchAsync("Condition", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(jsonDocument.RootElement));

        // Act
        var conditions = await _sut.SearchConditionsAsync("patient-1", "token", CancellationToken.None);

        // Assert
        await Assert.That(conditions.Count).IsEqualTo(1);
        await Assert.That(conditions[0].ClinicalStatus).IsNull();
        await Assert.That(conditions[0].Code).IsEqualTo("J06.9");
    }

    [Test]
    public async Task SearchConditionsAsync_HandlesMultipleClinicalStatuses()
    {
        // Arrange - FHIR allows multiple coding entries in clinicalStatus
        const string fhirBundle = """
            {
                "resourceType": "Bundle",
                "type": "searchset",
                "entry": [
                    {
                        "resource": {
                            "resourceType": "Condition",
                            "id": "cond-789",
                            "clinicalStatus": {
                                "coding": [
                                    {
                                        "system": "http://terminology.hl7.org/CodeSystem/condition-clinical",
                                        "code": "resolved",
                                        "display": "Resolved"
                                    },
                                    {
                                        "system": "http://example.org/custom",
                                        "code": "inactive"
                                    }
                                ]
                            },
                            "code": {
                                "coding": [
                                    {
                                        "system": "http://snomed.info/sct",
                                        "code": "195662009",
                                        "display": "Acute viral pharyngitis"
                                    }
                                ]
                            }
                        }
                    }
                ]
            }
            """;

        var jsonDocument = JsonDocument.Parse(fhirBundle);
        _httpClient.SearchAsync("Condition", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(jsonDocument.RootElement));

        // Act
        var conditions = await _sut.SearchConditionsAsync("patient-1", "token", CancellationToken.None);

        // Assert - should take the first coding entry
        await Assert.That(conditions.Count).IsEqualTo(1);
        await Assert.That(conditions[0].ClinicalStatus).IsEqualTo("resolved");
    }
}
