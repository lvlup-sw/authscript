// =============================================================================
// <copyright file="MutationTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.GraphQL;

using Gateway.API.Contracts;
using Gateway.API.GraphQL.Inputs;
using Gateway.API.GraphQL.Models;
using Gateway.API.GraphQL.Mutations;
using Gateway.API.Models;
using Gateway.API.Services;
using HotChocolate;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class MutationTests
{
    private readonly Mutation _mutation = new();
    private readonly IPARequestStore _store = Substitute.For<IPARequestStore>();
    private readonly IFhirDataAggregator _fhirAggregator = Substitute.For<IFhirDataAggregator>();
    private readonly IIntelligenceClient _intelligenceClient = Substitute.For<IIntelligenceClient>();
    private readonly ILogger<Mutation> _logger = Substitute.For<ILogger<Mutation>>();
    private readonly ReferenceDataService _refData = new();

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static PatientInput CreatePatientInput(string? fhirId = null) => new(
        Id: "patient-id-1",
        PatientId: "PAT-001",
        FhirId: fhirId,
        Name: "Jane Doe",
        Mrn: "MRN-12345",
        Dob: "1985-03-15",
        MemberId: "MEM-99999",
        Payer: "Blue Cross Blue Shield",
        Address: "123 Main St",
        Phone: "555-0100"
    );

    private static CreatePARequestInput CreateValidInput(string procedureCode = "72148", string? fhirId = null, string? providerId = null, string? diagnosisCode = "M54.5", string? diagnosisName = "Low Back Pain") => new(
        Patient: CreatePatientInput(fhirId),
        ProcedureCode: procedureCode,
        DiagnosisCode: diagnosisCode,
        DiagnosisName: diagnosisName,
        ProviderId: providerId
    );

    private static PARequestModel CreateStoredRequest(string id = "PA-001", string? fhirPatientId = null) => new()
    {
        Id = id,
        PatientId = "PAT-001",
        FhirPatientId = fhirPatientId,
        Patient = new PatientModel
        {
            Id = "patient-id-1",
            Name = "Jane Doe",
            Mrn = "MRN-12345",
            Dob = "1985-03-15",
            MemberId = "MEM-99999",
            Payer = "Blue Cross Blue Shield",
            Address = "123 Main St",
            Phone = "555-0100",
        },
        ProcedureCode = "72148",
        ProcedureName = "MRI Lumbar Spine w/o Contrast",
        Diagnosis = "Low Back Pain",
        DiagnosisCode = "M54.5",
        Payer = "Blue Cross Blue Shield",
        Provider = "Dr. Amanda Martinez",
        ProviderNpi = "1234567890",
        ServiceDate = "February 21, 2026",
        PlaceOfService = "Outpatient",
        ClinicalSummary = "",
        Status = "draft",
        Confidence = 0,
        CreatedAt = DateTimeOffset.UtcNow.ToString("o"),
        UpdatedAt = DateTimeOffset.UtcNow.ToString("o"),
        Criteria = [],
    };

    private static PAFormData CreateAnalysisResult(double confidenceScore = 0.85) => new()
    {
        PatientName = "Jane Doe",
        PatientDob = "1985-03-15",
        MemberId = "MEM-99999",
        DiagnosisCodes = ["M54.5"],
        ProcedureCode = "72148",
        ClinicalSummary = "Patient presents with chronic low back pain.",
        SupportingEvidence =
        [
            new EvidenceItem
            {
                CriterionId = "conservative-therapy",
                Status = "MET",
                Evidence = "Patient completed 6 weeks of physical therapy.",
                Source = "Observation",
                Confidence = 0.9,
            },
            new EvidenceItem
            {
                CriterionId = "imaging-indicated",
                Status = "NOT_MET",
                Evidence = "No prior imaging found in records.",
                Source = "Procedure",
                Confidence = 0.7,
            },
        ],
        Recommendation = "approve",
        ConfidenceScore = confidenceScore,
        FieldMappings = new Dictionary<string, string>
        {
            ["patient_name"] = "Jane Doe",
        },
    };

    private static ClinicalBundle CreateClinicalBundle(List<ConditionInfo>? conditions = null) => new()
    {
        PatientId = "FHIR-123",
        Conditions = conditions ?? [],
    };

    // ── CreatePARequest ─────────────────────────────────────────────────────

    [Test]
    public async Task CreatePARequest_ValidProcedureCode_CallsStoreWithCorrectData()
    {
        // Arrange
        var input = CreateValidInput(procedureCode: "72148");
        var expectedReturn = CreateStoredRequest();
        _store.CreateAsync(Arg.Any<PARequestModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedReturn);

        // Act
        var result = await _mutation.CreatePARequest(input, _store, _refData, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(expectedReturn.Id);

        // Verify store was called with correct model data
        await _store.Received(1).CreateAsync(
            Arg.Is<PARequestModel>(r =>
                r.ProcedureCode == "72148" &&
                r.ProcedureName == "MRI Lumbar Spine w/o Contrast" &&
                r.Diagnosis == "Low Back Pain" &&
                r.DiagnosisCode == "M54.5" &&
                r.Status == "draft" &&
                r.Confidence == 0 &&
                r.PlaceOfService == "Outpatient" &&
                r.PatientId == "PAT-001" &&
                r.Patient.Name == "Jane Doe" &&
                r.Patient.Mrn == "MRN-12345"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePARequest_UnknownProcedureCode_ThrowsArgumentException()
    {
        // Arrange
        var input = CreateValidInput(procedureCode: "UNKNOWN");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _mutation.CreatePARequest(input, _store, _refData, CancellationToken.None));

        await Assert.That(exception.Message).IsNotNull();
    }

    [Test]
    public async Task CreatePARequest_UsesFhirIdWhenPresent()
    {
        // Arrange
        var input = CreateValidInput(fhirId: "FHIR-999");
        _store.CreateAsync(Arg.Any<PARequestModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateStoredRequest());

        // Act
        await _mutation.CreatePARequest(input, _store, _refData, CancellationToken.None);

        // Assert - verify fhirPatientId argument is the FhirId, not the PatientId
        await _store.Received(1).CreateAsync(
            Arg.Any<PARequestModel>(),
            Arg.Is("FHIR-999"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePARequest_FallsBackToPatientId_WhenFhirIdNull()
    {
        // Arrange - FhirId is null, so it should fall back to PatientId
        var input = CreateValidInput(fhirId: null);
        _store.CreateAsync(Arg.Any<PARequestModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateStoredRequest());

        // Act
        await _mutation.CreatePARequest(input, _store, _refData, CancellationToken.None);

        // Assert - verify fhirPatientId falls back to PatientId ("PAT-001")
        await _store.Received(1).CreateAsync(
            Arg.Any<PARequestModel>(),
            Arg.Is("PAT-001"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePARequest_MedicationCode_ResolvesName()
    {
        // Arrange - use a medication code instead of procedure
        var input = CreateValidInput(procedureCode: "J1745");
        _store.CreateAsync(Arg.Any<PARequestModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateStoredRequest());

        // Act
        await _mutation.CreatePARequest(input, _store, _refData, CancellationToken.None);

        // Assert - verify procedure name comes from medication list
        await _store.Received(1).CreateAsync(
            Arg.Is<PARequestModel>(r => r.ProcedureName == "Infliximab (Remicade)"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePARequest_DefaultProvider_UsedWhenProviderIdNull()
    {
        // Arrange - ProviderId is null, so it should default to "DR001"
        var input = CreateValidInput(providerId: null);
        _store.CreateAsync(Arg.Any<PARequestModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateStoredRequest());

        // Act
        await _mutation.CreatePARequest(input, _store, _refData, CancellationToken.None);

        // Assert - DR001 is "Dr. Amanda Martinez" with NPI "1234567890"
        await _store.Received(1).CreateAsync(
            Arg.Is<PARequestModel>(r =>
                r.Provider == "Dr. Amanda Martinez" &&
                r.ProviderNpi == "1234567890"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreatePARequest_NullDiagnosis_UsesPendingPlaceholders()
    {
        // Arrange
        var input = CreateValidInput(diagnosisCode: null, diagnosisName: null);
        _store.CreateAsync(Arg.Any<PARequestModel>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateStoredRequest());

        // Act
        await _mutation.CreatePARequest(input, _store, _refData, CancellationToken.None);

        // Assert - diagnosis fields should default to placeholders
        await _store.Received(1).CreateAsync(
            Arg.Is<PARequestModel>(r =>
                r.Diagnosis == "Pending" &&
                r.DiagnosisCode == "PENDING"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // ── ProcessPARequest ────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPARequest_NonExistentId_ReturnsNull()
    {
        // Arrange
        _store.GetByIdAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((PARequestModel?)null);

        // Act
        var result = await _mutation.ProcessPARequest(
            "nonexistent", _store, _fhirAggregator, _intelligenceClient, _logger, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();

        // Verify aggregator was never called
        await _fhirAggregator.DidNotReceive().AggregateClinicalDataAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_UsesFhirPatientId_ForAggregator()
    {
        // Arrange - the stored request has a FhirPatientId that differs from PatientId
        var storedRequest = CreateStoredRequest(fhirPatientId: "FHIR-REAL-456");
        _store.GetByIdAsync("PA-001", Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        var clinicalBundle = CreateClinicalBundle();
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(clinicalBundle);

        var analysisResult = CreateAnalysisResult();
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysisResult);

        _store.ApplyAnalysisResultAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<CriterionModel>>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        // Act
        await _mutation.ProcessPARequest(
            "PA-001", _store, _fhirAggregator, _intelligenceClient, _logger, CancellationToken.None);

        // Assert - the aggregator must be called with FhirPatientId, NOT PatientId
        await _fhirAggregator.Received(1).AggregateClinicalDataAsync(
            Arg.Is("FHIR-REAL-456"), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_FallsBackToPatientId_WhenFhirPatientIdNull()
    {
        // Arrange - FhirPatientId is null, so it should use PatientId
        var storedRequest = CreateStoredRequest(fhirPatientId: null);
        _store.GetByIdAsync("PA-001", Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        var clinicalBundle = CreateClinicalBundle();
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(clinicalBundle);

        var analysisResult = CreateAnalysisResult();
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysisResult);

        _store.ApplyAnalysisResultAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<CriterionModel>>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        // Act
        await _mutation.ProcessPARequest(
            "PA-001", _store, _fhirAggregator, _intelligenceClient, _logger, CancellationToken.None);

        // Assert - aggregator called with PatientId ("PAT-001") since FhirPatientId is null
        await _fhirAggregator.Received(1).AggregateClinicalDataAsync(
            Arg.Is("PAT-001"), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_RoundsConfidenceScore()
    {
        // Arrange - 0.855 * 100 = 85.5, Math.Round should give 86 (not 85)
        var storedRequest = CreateStoredRequest(fhirPatientId: "FHIR-123");
        _store.GetByIdAsync("PA-001", Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(CreateClinicalBundle());

        var analysisResult = CreateAnalysisResult(confidenceScore: 0.855);
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysisResult);

        _store.ApplyAnalysisResultAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<CriterionModel>>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        // Act
        await _mutation.ProcessPARequest(
            "PA-001", _store, _fhirAggregator, _intelligenceClient, _logger, CancellationToken.None);

        // Assert - confidence should be rounded: 85.5 -> 86
        await _store.Received(1).ApplyAnalysisResultAsync(
            Arg.Is("PA-001"),
            Arg.Any<string>(),
            Arg.Is(86),
            Arg.Any<IReadOnlyList<CriterionModel>>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_MapsCriteriaFromAnalysis()
    {
        // Arrange
        var storedRequest = CreateStoredRequest(fhirPatientId: "FHIR-123");
        _store.GetByIdAsync("PA-001", Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(CreateClinicalBundle());

        var analysisResult = CreateAnalysisResult();
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysisResult);

        _store.ApplyAnalysisResultAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<CriterionModel>>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        // Act
        await _mutation.ProcessPARequest(
            "PA-001", _store, _fhirAggregator, _intelligenceClient, _logger, CancellationToken.None);

        // Assert - criteria mapped correctly: "MET" -> true, "NOT_MET" -> false
        await _store.Received(1).ApplyAnalysisResultAsync(
            Arg.Any<string>(),
            Arg.Is("Patient presents with chronic low back pain."),
            Arg.Any<int>(),
            Arg.Is<IReadOnlyList<CriterionModel>>(criteria =>
                criteria.Count == 2 &&
                criteria[0].Met == true &&
                criteria[0].Label == "conservative-therapy" &&
                criteria[0].Reason == "Patient completed 6 weeks of physical therapy." &&
                criteria[1].Met == false &&
                criteria[1].Label == "imaging-indicated" &&
                criteria[1].Reason == "No prior imaging found in records."),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_HttpException_ThrowsGraphQLException()
    {
        // Arrange
        var storedRequest = CreateStoredRequest(fhirPatientId: "FHIR-123");
        _store.GetByIdAsync("PA-001", Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("FHIR server unavailable"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<GraphQLException>(
            () => _mutation.ProcessPARequest(
                "PA-001", _store, _fhirAggregator, _intelligenceClient, _logger, CancellationToken.None));

        await Assert.That(exception.Message).IsEqualTo("Failed to process PA request: external service unavailable.");
    }

    [Test]
    public async Task ProcessPARequest_AutoDetectsDiagnosis_FromFirstActiveCondition()
    {
        // Arrange
        var storedRequest = CreateStoredRequest(fhirPatientId: "FHIR-123");
        _store.GetByIdAsync("PA-001", Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        var conditions = new List<ConditionInfo>
        {
            new() { Id = "cond-1", Code = "I10", Display = "Essential Hypertension", ClinicalStatus = "active" },
            new() { Id = "cond-2", Code = "E11.9", Display = "Type 2 Diabetes", ClinicalStatus = "active" },
        };
        var clinicalBundle = CreateClinicalBundle(conditions);
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(clinicalBundle);

        var analysisResult = CreateAnalysisResult();
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysisResult);

        _store.ApplyAnalysisResultAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<CriterionModel>>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        // Act
        await _mutation.ProcessPARequest(
            "PA-001", _store, _fhirAggregator, _intelligenceClient, _logger, CancellationToken.None);

        // Assert - first active condition should be used as diagnosis
        await _store.Received(1).ApplyAnalysisResultAsync(
            Arg.Is("PA-001"),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlyList<CriterionModel>>(),
            Arg.Is("I10"),
            Arg.Is("Essential Hypertension"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_NoConditions_PassesNullDiagnosis()
    {
        // Arrange
        var storedRequest = CreateStoredRequest(fhirPatientId: "FHIR-123");
        _store.GetByIdAsync("PA-001", Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        var clinicalBundle = CreateClinicalBundle(); // no conditions
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(clinicalBundle);

        var analysisResult = CreateAnalysisResult();
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(analysisResult);

        _store.ApplyAnalysisResultAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<CriterionModel>>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(storedRequest);

        // Act
        await _mutation.ProcessPARequest(
            "PA-001", _store, _fhirAggregator, _intelligenceClient, _logger, CancellationToken.None);

        // Assert - no conditions means null diagnosis passed (keeps existing placeholder)
        await _store.Received(1).ApplyAnalysisResultAsync(
            Arg.Is("PA-001"),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<IReadOnlyList<CriterionModel>>(),
            Arg.Is<string?>(s => s == null),
            Arg.Is<string?>(s => s == null),
            Arg.Any<CancellationToken>());
    }

    // ── UpdatePARequest ─────────────────────────────────────────────────────

    [Test]
    public async Task UpdatePARequest_DelegatesToStore()
    {
        // Arrange
        var criteriaInput = new List<CriterionInput>
        {
            new(Met: true, Label: "criterion-1", Reason: "Reason 1"),
            new(Met: false, Label: "criterion-2", Reason: "Reason 2"),
        };
        var input = new UpdatePARequestInput(
            Id: "PA-001",
            Diagnosis: "Updated Diagnosis",
            DiagnosisCode: "M54.2",
            ServiceDate: "2026-03-01",
            PlaceOfService: "Inpatient",
            ClinicalSummary: "Updated summary",
            Criteria: criteriaInput
        );

        var expectedResult = CreateStoredRequest();
        _store.UpdateFieldsAsync(
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<CriterionModel>?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _mutation.UpdatePARequest(input, _store, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await _store.Received(1).UpdateFieldsAsync(
            Arg.Is("PA-001"),
            Arg.Is("Updated Diagnosis"),
            Arg.Is("M54.2"),
            Arg.Is("2026-03-01"),
            Arg.Is("Inpatient"),
            Arg.Is("Updated summary"),
            Arg.Is<IReadOnlyList<CriterionModel>?>(c => c != null && c.Count == 2),
            Arg.Any<CancellationToken>());
    }

    // ── SubmitPARequest ─────────────────────────────────────────────────────

    [Test]
    public async Task SubmitPARequest_DelegatesToStore()
    {
        // Arrange
        var expectedResult = CreateStoredRequest();
        _store.SubmitAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _mutation.SubmitPARequest("PA-001", addReviewTimeSeconds: 30, store: _store, ct: CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await _store.Received(1).SubmitAsync(
            Arg.Is("PA-001"),
            Arg.Is(30),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitPARequest_DefaultReviewTime_IsZero()
    {
        // Arrange
        _store.SubmitAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CreateStoredRequest());

        // Act
        await _mutation.SubmitPARequest("PA-001", store: _store, ct: CancellationToken.None);

        // Assert - default addReviewTimeSeconds should be 0
        await _store.Received(1).SubmitAsync(
            Arg.Is("PA-001"),
            Arg.Is(0),
            Arg.Any<CancellationToken>());
    }

    // ── AddReviewTime ───────────────────────────────────────────────────────

    [Test]
    public async Task AddReviewTime_DelegatesToStore()
    {
        // Arrange
        var expectedResult = CreateStoredRequest();
        _store.AddReviewTimeAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _mutation.AddReviewTime("PA-001", 45, _store, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await _store.Received(1).AddReviewTimeAsync(
            Arg.Is("PA-001"),
            Arg.Is(45),
            Arg.Any<CancellationToken>());
    }

    // ── DeletePARequest ─────────────────────────────────────────────────────

    [Test]
    public async Task DeletePARequest_DelegatesToStore()
    {
        // Arrange
        _store.DeleteAsync("PA-001", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _mutation.DeletePARequest("PA-001", _store, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
        await _store.Received(1).DeleteAsync(
            Arg.Is("PA-001"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeletePARequest_NonExistentId_ReturnsFalse()
    {
        // Arrange
        _store.DeleteAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _mutation.DeletePARequest("nonexistent", _store, CancellationToken.None);

        // Assert
        await Assert.That(result).IsFalse();
    }
}
