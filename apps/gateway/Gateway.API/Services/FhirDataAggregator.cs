using Gateway.API.Abstractions;
using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Aggregates clinical data from Epic FHIR API by performing parallel queries
/// for patient demographics, conditions, observations, procedures, and documents.
/// </summary>
public sealed class FhirDataAggregator : IFhirDataAggregator
{
    private readonly IEpicFhirClient _fhirClient;
    private readonly ILogger<FhirDataAggregator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirDataAggregator"/> class.
    /// </summary>
    /// <param name="fhirClient">The Epic FHIR client for making API calls.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FhirDataAggregator(IEpicFhirClient fhirClient, ILogger<FhirDataAggregator> logger)
    {
        _fhirClient = fhirClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ClinicalBundle>> AggregateClinicalDataAsync(
        string patientId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Aggregating clinical data for patient {PatientId}", patientId);

        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
        var oneYearAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));

        // Parallel FHIR fetches for performance
        var patientTask = _fhirClient.GetPatientAsync(patientId, ct);
        var conditionsTask = _fhirClient.SearchConditionsAsync(patientId, ct);
        var observationsTask = _fhirClient.SearchObservationsAsync(patientId, sixMonthsAgo, ct);
        var proceduresTask = _fhirClient.SearchProceduresAsync(patientId, oneYearAgo, ct);
        var documentsTask = _fhirClient.SearchDocumentsAsync(patientId, ct);

        await Task.WhenAll(patientTask, conditionsTask, observationsTask, proceduresTask, documentsTask);

        var patientResult = await patientTask;
        var conditionsResult = await conditionsTask;
        var observationsResult = await observationsTask;
        var proceduresResult = await proceduresTask;
        var documentsResult = await documentsTask;

        // Patient is required - if it fails, propagate the error
        if (patientResult.IsFailure)
        {
            return patientResult.Error!;
        }

        // Other resources use default empty lists on failure (partial success)
        var bundle = new ClinicalBundle
        {
            PatientId = patientId,
            Patient = patientResult.Value,
            Conditions = conditionsResult.IsSuccess ? conditionsResult.Value!.ToList() : [],
            Observations = observationsResult.IsSuccess ? observationsResult.Value!.ToList() : [],
            Procedures = proceduresResult.IsSuccess ? proceduresResult.Value!.ToList() : [],
            Documents = documentsResult.IsSuccess ? documentsResult.Value!.ToList() : []
        };

        _logger.LogInformation(
            "Aggregated data: {Conditions} conditions, {Observations} observations, {Procedures} procedures, {Documents} documents",
            bundle.Conditions.Count,
            bundle.Observations.Count,
            bundle.Procedures.Count,
            bundle.Documents.Count);

        return bundle;
    }
}
