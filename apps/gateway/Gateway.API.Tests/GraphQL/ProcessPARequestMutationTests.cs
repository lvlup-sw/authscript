using Gateway.API.Contracts;
using Gateway.API.GraphQL.Models;
using Gateway.API.GraphQL.Mutations;
using Gateway.API.Models;
using Gateway.API.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Gateway.API.Tests.GraphQL;

[Category("Unit")]
public sealed class ProcessPARequestMutationTests
{
    private readonly MockDataService _mockData = new();
    private readonly IFhirDataAggregator _fhirAggregator = Substitute.For<IFhirDataAggregator>();
    private readonly IIntelligenceClient _intelligenceClient = Substitute.For<IIntelligenceClient>();
    private readonly Mutation _sut = new();

    private static ClinicalBundle CreateTestBundle(string patientId = "60178") => new()
    {
        PatientId = patientId,
        Patient = new PatientInfo
        {
            Id = patientId,
            GivenName = "Donna",
            FamilyName = "Sandbox",
            BirthDate = new DateOnly(1968, 3, 15),
            MemberId = "ATH60178"
        },
        Conditions = [new ConditionInfo { Id = "cond-1", Code = "M54.5", Display = "Low Back Pain", ClinicalStatus = "active" }],
    };

    private static PAFormData CreateTestFormData(
        string recommendation = "APPROVE",
        double confidence = 0.85,
        string procedureCode = "72148") => new()
    {
        PatientName = "Donna Sandbox",
        PatientDob = "1968-03-15",
        MemberId = "ATH60178",
        DiagnosisCodes = ["M54.5"],
        ProcedureCode = procedureCode,
        ClinicalSummary = "AI-generated clinical summary for testing.",
        SupportingEvidence =
        [
            new EvidenceItem
            {
                CriterionId = "conservative_therapy",
                Status = "MET",
                Evidence = "Patient completed 8 weeks of PT",
                Source = "Clinical Notes",
                Confidence = 0.95
            },
            new EvidenceItem
            {
                CriterionId = "failed_treatment",
                Status = "NOT_MET",
                Evidence = "No documentation of treatment failure",
                Source = "Chart Review",
                Confidence = 0.70
            },
            new EvidenceItem
            {
                CriterionId = "diagnosis_present",
                Status = "UNCLEAR",
                Evidence = "Diagnosis code needs verification",
                Source = "System",
                Confidence = 0.50
            }
        ],
        Recommendation = recommendation,
        ConfidenceScore = confidence,
        FieldMappings = new Dictionary<string, string> { ["PatientName"] = "Donna Sandbox" }
    };

    [Test]
    public async Task ProcessPARequest_CallsFhirAggregator_WithPatientId()
    {
        var paRequest = _mockData.GetPARequests().First();
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestBundle(paRequest.PatientId));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestFormData());

        await _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await _fhirAggregator.Received(1).AggregateClinicalDataAsync(
            paRequest.PatientId, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_CallsIntelligenceClient_WithBundleAndProcedureCode()
    {
        var paRequest = _mockData.GetPARequests().First();
        var bundle = CreateTestBundle(paRequest.PatientId);
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(bundle);
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestFormData(procedureCode: paRequest.ProcedureCode));

        await _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await _intelligenceClient.Received(1).AnalyzeAsync(
            Arg.Is<ClinicalBundle>(b => b.PatientId == paRequest.PatientId),
            paRequest.ProcedureCode,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_MapsEvidenceToMet_WhenStatusIsMET()
    {
        var paRequest = _mockData.GetPARequests().First();
        SetupMocks(paRequest.PatientId, paRequest.ProcedureCode);

        var result = await _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        var metCriterion = result!.Criteria.First(c => c.Label == "conservative_therapy");
        await Assert.That(metCriterion.Met).IsTrue();
    }

    [Test]
    public async Task ProcessPARequest_MapsEvidenceToNotMet_WhenStatusIsNOT_MET()
    {
        var paRequest = _mockData.GetPARequests().First();
        SetupMocks(paRequest.PatientId, paRequest.ProcedureCode);

        var result = await _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        var notMetCriterion = result!.Criteria.First(c => c.Label == "failed_treatment");
        await Assert.That(notMetCriterion.Met).IsFalse();
    }

    [Test]
    public async Task ProcessPARequest_MapsEvidenceToNull_WhenStatusIsUNCLEAR()
    {
        var paRequest = _mockData.GetPARequests().First();
        SetupMocks(paRequest.PatientId, paRequest.ProcedureCode);

        var result = await _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        var unclearCriterion = result!.Criteria.First(c => c.Label == "diagnosis_present");
        await Assert.That(unclearCriterion.Met).IsNull();
    }

    [Test]
    public async Task ProcessPARequest_SetsConfidence_FromScoreTimes100()
    {
        var paRequest = _mockData.GetPARequests().First();
        SetupMocks(paRequest.PatientId, paRequest.ProcedureCode, confidence: 0.85);

        var result = await _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Confidence).IsEqualTo(85);
    }

    [Test]
    public async Task ProcessPARequest_ReturnsNull_WhenPARequestNotFound()
    {
        var result = await _sut.ProcessPARequest("NONEXISTENT", _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await Assert.That(result).IsNull();
        // Should NOT call any services
        await _fhirAggregator.DidNotReceive().AggregateClinicalDataAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ProcessPARequest_WhenFhirAggregatorThrows_PropagatesException()
    {
        var paRequest = _mockData.GetPARequests().First();
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("FHIR service unavailable"));

        var act = () => _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await Assert.That(act).ThrowsExactly<HttpRequestException>();
    }

    [Test]
    public async Task ProcessPARequest_WhenIntelligenceThrowsHttpRequestException_PropagatesException()
    {
        var paRequest = _mockData.GetPARequests().First();
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestBundle(paRequest.PatientId));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Intelligence service returned 500"));

        var act = () => _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, CancellationToken.None);

        await Assert.That(act).ThrowsExactly<HttpRequestException>();
    }

    [Test]
    public async Task ProcessPARequest_WhenCancelled_ThrowsOperationCanceledException()
    {
        var paRequest = _mockData.GetPARequests().First();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var act = () => _sut.ProcessPARequest(paRequest.Id, _mockData, _fhirAggregator, _intelligenceClient, cts.Token);

        await Assert.That(act).ThrowsExactly<OperationCanceledException>();
    }

    private void SetupMocks(string patientId, string procedureCode, double confidence = 0.85)
    {
        _fhirAggregator.AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestBundle(patientId));
        _intelligenceClient.AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestFormData(confidence: confidence, procedureCode: procedureCode));
    }
}
