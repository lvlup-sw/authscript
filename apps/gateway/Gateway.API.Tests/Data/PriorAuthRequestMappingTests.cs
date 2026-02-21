// =============================================================================
// <copyright file="PriorAuthRequestMappingTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.Data;

using Gateway.API.Data.Entities;
using Gateway.API.Data.Mappings;
using Gateway.API.GraphQL.Models;

/// <summary>
/// Tests for <see cref="PriorAuthRequestMappings"/>.
/// </summary>
public class PriorAuthRequestMappingTests
{
    private static readonly DateTimeOffset TestCreatedAt = new(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TestUpdatedAt = new(2025, 6, 15, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TestReadyAt = new(2025, 6, 15, 10, 35, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TestSubmittedAt = new(2025, 6, 15, 11, 5, 0, TimeSpan.Zero);

    private static PriorAuthRequestEntity CreateTestEntity() => new()
    {
        Id = "pa-001",
        PatientId = "60178",
        FhirPatientId = "a-195900.E-60178",
        PatientName = "John Doe",
        PatientMrn = "MRN001",
        PatientDob = "1980-01-15",
        PatientMemberId = "MEM123",
        PatientPayer = "Aetna",
        PatientAddress = "123 Main St",
        PatientPhone = "555-0100",
        ProcedureCode = "27447",
        ProcedureName = "Total Knee Replacement",
        DiagnosisCode = "M17.11",
        DiagnosisName = "Primary osteoarthritis, right knee",
        ProviderId = "prov-1",
        ProviderName = "Dr. Smith",
        ProviderNpi = "1234567890",
        ServiceDate = "2025-07-01",
        PlaceOfService = "Office",
        ClinicalSummary = "Patient requires knee replacement.",
        Status = "ready",
        Confidence = 85,
        CriteriaJson = """[{"met":true,"label":"BMI > 30","reason":"Patient BMI is 32"},{"met":false,"label":"Failed PT","reason":"No PT records found"}]""",
        CreatedAt = TestCreatedAt,
        UpdatedAt = TestUpdatedAt,
        ReadyAt = TestReadyAt,
        SubmittedAt = TestSubmittedAt,
        ReviewTimeSeconds = 120,
    };

    private static PARequestModel CreateTestModel() => new()
    {
        Id = "pa-001",
        PatientId = "60178",
        Patient = new PatientModel
        {
            Id = "60178",
            Name = "John Doe",
            Mrn = "MRN001",
            Dob = "1980-01-15",
            MemberId = "MEM123",
            Payer = "Aetna",
            Address = "123 Main St",
            Phone = "555-0100",
        },
        ProcedureCode = "27447",
        ProcedureName = "Total Knee Replacement",
        DiagnosisCode = "M17.11",
        Diagnosis = "Primary osteoarthritis, right knee",
        Payer = "Aetna",
        Provider = "Dr. Smith",
        ProviderNpi = "1234567890",
        ServiceDate = "2025-07-01",
        PlaceOfService = "Office",
        ClinicalSummary = "Patient requires knee replacement.",
        Status = "ready",
        Confidence = 85,
        CreatedAt = TestCreatedAt.ToString("o"),
        UpdatedAt = TestUpdatedAt.ToString("o"),
        ReadyAt = TestReadyAt.ToString("o"),
        SubmittedAt = TestSubmittedAt.ToString("o"),
        ReviewTimeSeconds = 120,
        Criteria =
        [
            new CriterionModel { Met = true, Label = "BMI > 30", Reason = "Patient BMI is 32" },
            new CriterionModel { Met = false, Label = "Failed PT", Reason = "No PT records found" },
        ],
    };

    [Test]
    public async Task ToModel_MapsAllScalarFields()
    {
        // Arrange
        var entity = CreateTestEntity();

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.Id).IsEqualTo("pa-001");
        await Assert.That(model.PatientId).IsEqualTo("60178");
        await Assert.That(model.ProcedureCode).IsEqualTo("27447");
        await Assert.That(model.ProcedureName).IsEqualTo("Total Knee Replacement");
        await Assert.That(model.DiagnosisCode).IsEqualTo("M17.11");
        await Assert.That(model.Diagnosis).IsEqualTo("Primary osteoarthritis, right knee");
        await Assert.That(model.Payer).IsEqualTo("Aetna");
        await Assert.That(model.Provider).IsEqualTo("Dr. Smith");
        await Assert.That(model.ProviderNpi).IsEqualTo("1234567890");
        await Assert.That(model.ServiceDate).IsEqualTo("2025-07-01");
        await Assert.That(model.PlaceOfService).IsEqualTo("Office");
        await Assert.That(model.ClinicalSummary).IsEqualTo("Patient requires knee replacement.");
        await Assert.That(model.Status).IsEqualTo("ready");
        await Assert.That(model.Confidence).IsEqualTo(85);
        await Assert.That(model.ReviewTimeSeconds).IsEqualTo(120);
    }

    [Test]
    public async Task ToModel_ReconstructsPatientModel()
    {
        // Arrange
        var entity = CreateTestEntity();

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.Patient).IsNotNull();
        await Assert.That(model.Patient.Id).IsEqualTo("60178");
        await Assert.That(model.Patient.Name).IsEqualTo("John Doe");
        await Assert.That(model.Patient.Mrn).IsEqualTo("MRN001");
        await Assert.That(model.Patient.Dob).IsEqualTo("1980-01-15");
        await Assert.That(model.Patient.MemberId).IsEqualTo("MEM123");
        await Assert.That(model.Patient.Payer).IsEqualTo("Aetna");
        await Assert.That(model.Patient.Address).IsEqualTo("123 Main St");
        await Assert.That(model.Patient.Phone).IsEqualTo("555-0100");
    }

    [Test]
    public async Task ToModel_DeserializesCriteriaJson()
    {
        // Arrange
        var entity = CreateTestEntity();

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.Criteria).HasCount().EqualTo(2);
        await Assert.That(model.Criteria[0].Met).IsEqualTo(true);
        await Assert.That(model.Criteria[0].Label).IsEqualTo("BMI > 30");
        await Assert.That(model.Criteria[0].Reason).IsEqualTo("Patient BMI is 32");
        await Assert.That(model.Criteria[1].Met).IsEqualTo(false);
        await Assert.That(model.Criteria[1].Label).IsEqualTo("Failed PT");
        await Assert.That(model.Criteria[1].Reason).IsEqualTo("No PT records found");
    }

    [Test]
    public async Task ToModel_HandlesNullCriteriaJson()
    {
        // Arrange
        var entity = CreateTestEntity();
        entity.CriteriaJson = null;

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.Criteria).IsNotNull();
        await Assert.That(model.Criteria).HasCount().EqualTo(0);
    }

    [Test]
    public async Task ToModel_FormatsTimestampsAsIsoStrings()
    {
        // Arrange
        var entity = CreateTestEntity();

        // Act
        var model = entity.ToModel();

        // Assert
        await Assert.That(model.CreatedAt).IsEqualTo(TestCreatedAt.ToString("o"));
        await Assert.That(model.UpdatedAt).IsEqualTo(TestUpdatedAt.ToString("o"));
        await Assert.That(model.ReadyAt).IsEqualTo(TestReadyAt.ToString("o"));
        await Assert.That(model.SubmittedAt).IsEqualTo(TestSubmittedAt.ToString("o"));
    }

    [Test]
    public async Task ToEntity_MapsAllScalarFields()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var entity = model.ToEntity(fhirPatientId: "a-195900.E-60178");

        // Assert
        await Assert.That(entity.Id).IsEqualTo("pa-001");
        await Assert.That(entity.PatientId).IsEqualTo("60178");
        await Assert.That(entity.FhirPatientId).IsEqualTo("a-195900.E-60178");
        await Assert.That(entity.ProcedureCode).IsEqualTo("27447");
        await Assert.That(entity.ProcedureName).IsEqualTo("Total Knee Replacement");
        await Assert.That(entity.DiagnosisCode).IsEqualTo("M17.11");
        await Assert.That(entity.DiagnosisName).IsEqualTo("Primary osteoarthritis, right knee");
        await Assert.That(entity.ProviderName).IsEqualTo("Dr. Smith");
        await Assert.That(entity.ProviderNpi).IsEqualTo("1234567890");
        await Assert.That(entity.ServiceDate).IsEqualTo("2025-07-01");
        await Assert.That(entity.PlaceOfService).IsEqualTo("Office");
        await Assert.That(entity.ClinicalSummary).IsEqualTo("Patient requires knee replacement.");
        await Assert.That(entity.Status).IsEqualTo("ready");
        await Assert.That(entity.Confidence).IsEqualTo(85);
        await Assert.That(entity.ReviewTimeSeconds).IsEqualTo(120);
        await Assert.That(entity.CreatedAt).IsEqualTo(TestCreatedAt);
        await Assert.That(entity.UpdatedAt).IsEqualTo(TestUpdatedAt);
        await Assert.That(entity.ReadyAt).IsEqualTo(TestReadyAt);
        await Assert.That(entity.SubmittedAt).IsEqualTo(TestSubmittedAt);
    }

    [Test]
    public async Task ToEntity_FlattensPatientModel()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var entity = model.ToEntity(fhirPatientId: "a-195900.E-60178");

        // Assert
        await Assert.That(entity.PatientName).IsEqualTo("John Doe");
        await Assert.That(entity.PatientMrn).IsEqualTo("MRN001");
        await Assert.That(entity.PatientDob).IsEqualTo("1980-01-15");
        await Assert.That(entity.PatientMemberId).IsEqualTo("MEM123");
        await Assert.That(entity.PatientPayer).IsEqualTo("Aetna");
        await Assert.That(entity.PatientAddress).IsEqualTo("123 Main St");
        await Assert.That(entity.PatientPhone).IsEqualTo("555-0100");
    }

    [Test]
    public async Task ToEntity_SerializesCriteriaToJson()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var entity = model.ToEntity();

        // Assert
        await Assert.That(entity.CriteriaJson).IsNotNull();

        // Verify round-trip: deserialize back and check
        var roundTripped = entity.ToModel();
        await Assert.That(roundTripped.Criteria).HasCount().EqualTo(2);
        await Assert.That(roundTripped.Criteria[0].Label).IsEqualTo("BMI > 30");
        await Assert.That(roundTripped.Criteria[1].Label).IsEqualTo("Failed PT");
    }

    [Test]
    public async Task ToEntity_UsesFhirPatientIdParam()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var entity = model.ToEntity(fhirPatientId: "custom-fhir-id");

        // Assert
        await Assert.That(entity.FhirPatientId).IsEqualTo("custom-fhir-id");
    }

    [Test]
    public async Task ToEntity_FallsBackToPatientIdForFhir()
    {
        // Arrange
        var model = CreateTestModel();

        // Act — no fhirPatientId provided, should use Patient.Id
        var entity = model.ToEntity();

        // Assert
        await Assert.That(entity.FhirPatientId).IsEqualTo("60178");
    }
}
