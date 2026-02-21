using System.Text.Json;
using Gateway.API.Models;
using Gateway.API.Services;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for IntelligenceClient serialization: verifying that BuildAnalyzeRequest()
/// correctly maps C# models to the snake_case DTO that the Python Intelligence service expects.
/// </summary>
public class IntelligenceClientSerializationTests
{
    private static ClinicalBundle CreateFullBundle()
    {
        return new ClinicalBundle
        {
            PatientId = "patient-123",
            Patient = new PatientInfo
            {
                Id = "patient-123",
                GivenName = "Donna",
                FamilyName = "Sandbox",
                BirthDate = new DateOnly(1968, 3, 15),
                Gender = "female",
                MemberId = "ATH60178"
            },
            Conditions =
            [
                new ConditionInfo
                {
                    Id = "cond-1",
                    Code = "M54.5",
                    CodeSystem = "http://hl7.org/fhir/sid/icd-10-cm",
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
                    CodeSystem = "http://loinc.org",
                    Display = "Smoking status",
                    Value = "Never smoker",
                    Unit = "N/A"
                }
            ],
            Procedures =
            [
                new ProcedureInfo
                {
                    Id = "proc-1",
                    Code = "99213",
                    CodeSystem = "http://www.ama-assn.org/go/cpt",
                    Display = "Office visit",
                    Status = "completed"
                }
            ]
        };
    }

    [Test]
    public async Task BuildAnalyzeRequest_MapsPatientFullName_ToNameField()
    {
        // Arrange
        var bundle = CreateFullBundle();

        // Act
        var dto = IntelligenceClient.BuildAnalyzeRequest(bundle, "72148");

        // Assert
        await Assert.That(dto.ClinicalData.Patient).IsNotNull();
        await Assert.That(dto.ClinicalData.Patient!.Name).IsEqualTo("Donna Sandbox");
    }

    [Test]
    public async Task BuildAnalyzeRequest_MapsPatientBirthDate_ToIsoString()
    {
        // Arrange
        var bundle = CreateFullBundle();

        // Act
        var dto = IntelligenceClient.BuildAnalyzeRequest(bundle, "72148");

        // Assert
        await Assert.That(dto.ClinicalData.Patient!.BirthDate).IsEqualTo("1968-03-15");
    }

    [Test]
    public async Task BuildAnalyzeRequest_MapsConditions_ToSnakeCaseKeys()
    {
        // Arrange
        var bundle = CreateFullBundle();

        // Act
        var dto = IntelligenceClient.BuildAnalyzeRequest(bundle, "72148");

        // Assert
        await Assert.That(dto.ClinicalData.Conditions).HasCount().EqualTo(1);
        var condition = dto.ClinicalData.Conditions[0];
        await Assert.That(condition.Code).IsEqualTo("M54.5");
        await Assert.That(condition.System).IsEqualTo("http://hl7.org/fhir/sid/icd-10-cm");
        await Assert.That(condition.Display).IsEqualTo("Low back pain");
        await Assert.That(condition.ClinicalStatus).IsEqualTo("active");
    }

    [Test]
    public async Task BuildAnalyzeRequest_MapsObservations_WithAllFields()
    {
        // Arrange
        var bundle = CreateFullBundle();

        // Act
        var dto = IntelligenceClient.BuildAnalyzeRequest(bundle, "72148");

        // Assert
        await Assert.That(dto.ClinicalData.Observations).HasCount().EqualTo(1);
        var obs = dto.ClinicalData.Observations[0];
        await Assert.That(obs.Code).IsEqualTo("72166-2");
        await Assert.That(obs.System).IsEqualTo("http://loinc.org");
        await Assert.That(obs.Display).IsEqualTo("Smoking status");
        await Assert.That(obs.Value).IsEqualTo("Never smoker");
        await Assert.That(obs.Unit).IsEqualTo("N/A");
    }

    [Test]
    public async Task BuildAnalyzeRequest_WithNullPatient_SetsNullPatientField()
    {
        // Arrange
        var bundle = new ClinicalBundle
        {
            PatientId = "patient-123",
            Patient = null,
            Conditions = [],
            Observations = [],
            Procedures = []
        };

        // Act
        var dto = IntelligenceClient.BuildAnalyzeRequest(bundle, "72148");

        // Assert
        await Assert.That(dto.ClinicalData.Patient).IsNull();
        await Assert.That(dto.PatientId).IsEqualTo("patient-123");
        await Assert.That(dto.ProcedureCode).IsEqualTo("72148");
    }

    [Test]
    public async Task SerializeRequest_UsesSnakeCaseNaming()
    {
        // Arrange
        var bundle = CreateFullBundle();
        var dto = IntelligenceClient.BuildAnalyzeRequest(bundle, "72148");

        // Act
        var json = JsonSerializer.Serialize(dto, IntelligenceClient.SnakeCaseSerializerOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert - top-level keys are snake_case
        await Assert.That(root.TryGetProperty("patient_id", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("procedure_code", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("clinical_data", out _)).IsTrue();

        // Assert - nested patient keys are snake_case
        var clinicalData = root.GetProperty("clinical_data");
        var patient = clinicalData.GetProperty("patient");
        await Assert.That(patient.TryGetProperty("name", out _)).IsTrue();
        await Assert.That(patient.TryGetProperty("birth_date", out _)).IsTrue();
        await Assert.That(patient.TryGetProperty("gender", out _)).IsTrue();
        await Assert.That(patient.TryGetProperty("member_id", out _)).IsTrue();

        // Assert - condition keys are snake_case
        var conditions = clinicalData.GetProperty("conditions");
        var firstCondition = conditions[0];
        await Assert.That(firstCondition.TryGetProperty("clinical_status", out _)).IsTrue();
    }
}
