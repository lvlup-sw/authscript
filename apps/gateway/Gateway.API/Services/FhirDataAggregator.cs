using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Models;
using Microsoft.Extensions.Options;

namespace Gateway.API.Services;

/// <summary>
/// Aggregates clinical data from FHIR API by performing parallel queries
/// for patient demographics, conditions, observations, procedures, documents, and service requests.
/// </summary>
public sealed class FhirDataAggregator : IFhirDataAggregator
{
    private readonly IFhirClient _fhirClient;
    private readonly ClinicalQueryOptions _options;
    private readonly ILogger<FhirDataAggregator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FhirDataAggregator"/> class.
    /// </summary>
    /// <param name="fhirClient">The FHIR client for making API calls.</param>
    /// <param name="options">Clinical query configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FhirDataAggregator(
        IFhirClient fhirClient,
        IOptions<ClinicalQueryOptions> options,
        ILogger<FhirDataAggregator> logger)
    {
        _fhirClient = fhirClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClinicalBundle> AggregateClinicalDataAsync(
        string patientId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aggregating clinical data for patient {PatientId}", patientId);

        var observationSince = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-_options.ObservationLookbackMonths));
        var procedureSince = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-_options.ProcedureLookbackMonths));

        // Parallel FHIR fetches for performance
        var patientTask = _fhirClient.GetPatientAsync(patientId, accessToken, cancellationToken);
        var conditionsTask = _fhirClient.SearchConditionsAsync(patientId, accessToken, cancellationToken);
        var observationsTask = _fhirClient.SearchObservationsAsync(patientId, observationSince, accessToken, cancellationToken);
        var proceduresTask = _fhirClient.SearchProceduresAsync(patientId, procedureSince, accessToken, cancellationToken);
        var documentsTask = _fhirClient.SearchDocumentsAsync(patientId, accessToken, cancellationToken);
        var serviceRequestsTask = _fhirClient.SearchServiceRequestsAsync(patientId, null, accessToken, cancellationToken);

        await Task.WhenAll(patientTask, conditionsTask, observationsTask, proceduresTask, documentsTask, serviceRequestsTask);

        var bundle = new ClinicalBundle
        {
            PatientId = patientId,
            Patient = await patientTask,
            Conditions = await conditionsTask,
            Observations = await observationsTask,
            Procedures = await proceduresTask,
            Documents = await documentsTask,
            ServiceRequests = await serviceRequestsTask
        };

        _logger.LogInformation(
            "Aggregated data: {Conditions} conditions, {Observations} observations, {Procedures} procedures, {Documents} documents, {ServiceRequests} service requests",
            bundle.Conditions.Count,
            bundle.Observations.Count,
            bundle.Procedures.Count,
            bundle.Documents.Count,
            bundle.ServiceRequests.Count);

        return bundle;
    }
}
