using Gateway.API.Contracts;
using Gateway.API.Contracts.Http;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Processes finished encounters by hydrating clinical context and generating PA forms.
/// Orchestrates data aggregation from FHIR, analysis by Intelligence service,
/// PDF generation, and notification broadcasting.
/// </summary>
public sealed class EncounterProcessor : IEncounterProcessor
{
    private readonly IFhirDataAggregator _aggregator;
    private readonly IIntelligenceClient _intelligenceClient;
    private readonly IPdfFormStamper _pdfStamper;
    private readonly IAnalysisResultStore _resultStore;
    private readonly INotificationHub _notificationHub;
    private readonly ITokenAcquisitionStrategy _tokenStrategy;
    private readonly ILogger<EncounterProcessor> _logger;

    // Default procedure code when not specified by encounter
    // In production, this would be extracted from the encounter ServiceRequest or claim
    private const string DefaultProcedureCode = "72148";

    /// <summary>
    /// Initializes a new instance of the <see cref="EncounterProcessor"/> class.
    /// </summary>
    /// <param name="aggregator">FHIR data aggregator for hydrating clinical context.</param>
    /// <param name="intelligenceClient">Intelligence client for PA analysis.</param>
    /// <param name="pdfStamper">PDF form stamper for generating PA forms.</param>
    /// <param name="resultStore">Result store for caching generated PDFs.</param>
    /// <param name="notificationHub">Notification hub for broadcasting completion events.</param>
    /// <param name="tokenStrategy">Token acquisition strategy for obtaining access tokens.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public EncounterProcessor(
        IFhirDataAggregator aggregator,
        IIntelligenceClient intelligenceClient,
        IPdfFormStamper pdfStamper,
        IAnalysisResultStore resultStore,
        INotificationHub notificationHub,
        ITokenAcquisitionStrategy tokenStrategy,
        ILogger<EncounterProcessor> logger)
    {
        _aggregator = aggregator;
        _intelligenceClient = intelligenceClient;
        _pdfStamper = pdfStamper;
        _resultStore = resultStore;
        _notificationHub = notificationHub;
        _tokenStrategy = tokenStrategy;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ProcessEncounterAsync(string encounterId, string patientId, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing encounter {EncounterId} for patient {PatientId}",
            encounterId,
            patientId);

        // Acquire access token dynamically via token strategy
        var accessToken = _tokenStrategy.CanHandle
            ? await _tokenStrategy.AcquireTokenAsync(ct)
            : null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogError("No access token available for encounter {EncounterId}", encounterId);
            return;
        }

        var transactionId = $"pa-{encounterId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        try
        {
            // Step 1: Hydrate clinical context via FHIR data aggregator
            var clinicalBundle = await _aggregator.AggregateClinicalDataAsync(
                patientId,
                accessToken,
                ct);

            _logger.LogInformation(
                "Hydrated clinical bundle with {Conditions} conditions, {Observations} observations, {Procedures} procedures",
                clinicalBundle.Conditions.Count,
                clinicalBundle.Observations.Count,
                clinicalBundle.Procedures.Count);

            // Step 2: Send to Intelligence service for PA analysis
            var formData = await _intelligenceClient.AnalyzeAsync(
                clinicalBundle,
                DefaultProcedureCode,
                ct);

            _logger.LogInformation(
                "Intelligence analysis complete. Recommendation: {Recommendation}, Confidence: {Confidence:P0}",
                formData.Recommendation,
                formData.ConfidenceScore);

            // Step 3: Generate PDF from form data
            var pdfBytes = await _pdfStamper.StampFormAsync(formData, ct);

            _logger.LogInformation(
                "Generated PDF form: {Size} bytes for encounter {EncounterId}",
                pdfBytes.Length,
                encounterId);

            // Step 4: Store PDF in result store with transaction ID
            var cacheKey = $"{encounterId}:{transactionId}";
            await _resultStore.SetCachedPdfAsync(cacheKey, pdfBytes, ct);

            _logger.LogInformation(
                "Stored PDF with cache key {CacheKey}",
                cacheKey);

            // Step 5: Notify subscribers that PA form is ready
            var notification = new Notification(
                Type: "PA_FORM_READY",
                TransactionId: transactionId,
                EncounterId: encounterId,
                PatientId: patientId,
                Message: $"Prior authorization form ready. Recommendation: {formData.Recommendation}"
            );

            await _notificationHub.WriteAsync(notification, ct);

            _logger.LogInformation(
                "Notification sent for encounter {EncounterId}: {Type}",
                encounterId,
                notification.Type);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Service error for encounter {EncounterId}: {Message}",
                encounterId,
                ex.Message);
            // Graceful handling - don't propagate, just log
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing encounter {EncounterId}: {Message}",
                encounterId,
                ex.Message);
            // Graceful handling - don't propagate, just log
        }
    }
}
