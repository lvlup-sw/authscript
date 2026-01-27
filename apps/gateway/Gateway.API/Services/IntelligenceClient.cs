using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// STUB: Intelligence client that returns mock PA analysis data.
/// Production implementation will call the Intelligence service HTTP API.
/// </summary>
public sealed class IntelligenceClient : IIntelligenceClient
{
    private readonly ILogger<IntelligenceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntelligenceClient"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public IntelligenceClient(ILogger<IntelligenceClient> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<PAFormData> AnalyzeAsync(
        ClinicalBundle clinicalBundle,
        string procedureCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "STUB: Returning mock analysis for ProcedureCode={ProcedureCode}",
            procedureCode);

        var patientName = clinicalBundle.Patient?.FullName ?? "Unknown Patient";
        var patientDob = clinicalBundle.Patient?.BirthDate?.ToString("yyyy-MM-dd") ?? "Unknown";
        var memberId = clinicalBundle.Patient?.MemberId ?? "Unknown";
        var diagnosisCodes = clinicalBundle.Conditions
            .Select(c => c.Code)
            .Where(c => !string.IsNullOrEmpty(c))
            .DefaultIfEmpty("M54.5")
            .ToList();

        var result = new PAFormData
        {
            PatientName = patientName,
            PatientDob = patientDob,
            MemberId = memberId,
            DiagnosisCodes = diagnosisCodes!,
            ProcedureCode = procedureCode,
            ClinicalSummary = "STUB: Mock clinical summary for demo purposes. " +
                "Production will generate AI-powered clinical justification.",
            SupportingEvidence =
            [
                new EvidenceItem
                {
                    CriterionId = "diagnosis_present",
                    Status = "MET",
                    Evidence = "STUB: Qualifying diagnosis code found",
                    Source = "Stub implementation",
                    Confidence = 0.95
                },
                new EvidenceItem
                {
                    CriterionId = "conservative_therapy",
                    Status = "MET",
                    Evidence = "STUB: Conservative therapy documented",
                    Source = "Stub implementation",
                    Confidence = 0.90
                }
            ],
            Recommendation = "APPROVE",
            ConfidenceScore = 0.95,
            FieldMappings = new Dictionary<string, string>
            {
                ["PatientName"] = patientName,
                ["PatientDOB"] = patientDob,
                ["MemberID"] = memberId,
                ["PrimaryDiagnosis"] = diagnosisCodes.FirstOrDefault() ?? "M54.5",
                ["ProcedureCode"] = procedureCode,
                ["ClinicalJustification"] = "STUB: Clinical justification",
                ["RequestedDateOfService"] = DateTime.Today.ToString("yyyy-MM-dd")
            }
        };

        return Task.FromResult(result);
    }
}
