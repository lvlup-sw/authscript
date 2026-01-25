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
    public async Task<ClinicalBundle> AggregateClinicalDataAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aggregating clinical data for patient {PatientId}", patientId);

        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
        var oneYearAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));

        // Parallel FHIR fetches for performance
        var patientTask = _fhirClient.GetPatientAsync(patientId, accessToken, cancellationToken);
        var conditionsTask = _fhirClient.SearchConditionsAsync(patientId, accessToken, cancellationToken);
        var observationsTask = _fhirClient.SearchObservationsAsync(patientId, sixMonthsAgo, accessToken, cancellationToken);
        var proceduresTask = _fhirClient.SearchProceduresAsync(patientId, oneYearAgo, accessToken, cancellationToken);
        var documentsTask = _fhirClient.SearchDocumentsAsync(patientId, accessToken, cancellationToken);

        await Task.WhenAll(patientTask, conditionsTask, observationsTask, proceduresTask, documentsTask);

        var bundle = new ClinicalBundle
        {
            PatientId = patientId,
            Patient = await patientTask,
            Conditions = await conditionsTask,
            Observations = await observationsTask,
            Procedures = await proceduresTask,
            Documents = await documentsTask
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
