using Gateway.API.Contracts;
using Gateway.API.Models;

namespace Gateway.API.Services;

/// <summary>
/// Processes finished encounters by hydrating clinical context and generating PA forms.
/// Orchestrates data aggregation from FHIR, analysis by Intelligence service,
/// PDF generation, and notification broadcasting.
/// Token management is handled internally by FhirDataAggregator via IFhirTokenProvider.
/// </summary>
public sealed class EncounterProcessor : IEncounterProcessor
{
    private readonly IFhirDataAggregator _aggregator;
    private readonly IIntelligenceClient _intelligenceClient;
    private readonly IPdfFormStamper _pdfStamper;
    private readonly IAnalysisResultStore _resultStore;
    private readonly INotificationHub _notificationHub;
    private readonly IWorkItemStore _workItemStore;
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
    /// <param name="workItemStore">Work item store for updating work item status.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public EncounterProcessor(
        IFhirDataAggregator aggregator,
        IIntelligenceClient intelligenceClient,
        IPdfFormStamper pdfStamper,
        IAnalysisResultStore resultStore,
        INotificationHub notificationHub,
        IWorkItemStore workItemStore,
        ILogger<EncounterProcessor> logger)
    {
        _aggregator = aggregator;
        _intelligenceClient = intelligenceClient;
        _pdfStamper = pdfStamper;
        _resultStore = resultStore;
        _notificationHub = notificationHub;
        _workItemStore = workItemStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ProcessAsync(EncounterCompletedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing encounter {EncounterId} for patient {PatientId} (WorkItem: {WorkItemId})",
            evt.EncounterId,
            evt.PatientId,
            evt.WorkItemId);

        var transactionId = $"pa-{evt.EncounterId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        try
        {
            // Step 1: Hydrate clinical context via FHIR data aggregator
            var clinicalBundle = await _aggregator.AggregateClinicalDataAsync(
                evt.PatientId,
                evt.EncounterId,
                ct);

            _logger.LogInformation(
                "Hydrated clinical bundle with {Conditions} conditions, {Observations} observations, {Procedures} procedures",
                clinicalBundle.Conditions.Count,
                clinicalBundle.Observations.Count,
                clinicalBundle.Procedures.Count);

            // Step 2: Extract procedure code from ServiceRequest (fall back to default)
            var procedureCode = clinicalBundle.ServiceRequests
                .FirstOrDefault(sr => sr.Status == "active")
                ?.Code?.Coding?.FirstOrDefault()?.Code
                ?? DefaultProcedureCode;

            // Step 3: Send to Intelligence service for PA analysis
            var formData = await _intelligenceClient.AnalyzeAsync(
                clinicalBundle,
                procedureCode,
                ct);

            _logger.LogInformation(
                "Intelligence analysis complete. Recommendation: {Recommendation}, Confidence: {Confidence:P0}",
                formData.Recommendation,
                formData.ConfidenceScore);

            // Step 4: Determine work item status based on recommendation
            var status = RecommendationMapper.MapToStatus(formData.Recommendation, formData.ConfidenceScore);

            // Step 4: Update work item with ServiceRequestId, ProcedureCode, and status
            var existingWorkItem = await _workItemStore.GetByIdAsync(evt.WorkItemId, ct);
            string? serviceRequestId = null;

            if (existingWorkItem is not null)
            {
                // Extract ServiceRequestId from the first active ServiceRequest in the bundle
                serviceRequestId = clinicalBundle.ServiceRequests
                    .FirstOrDefault(sr => sr.Status == "active")?.Id;

                var updatedWorkItem = existingWorkItem with
                {
                    ServiceRequestId = serviceRequestId,
                    ProcedureCode = formData.ProcedureCode,
                    Status = status,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                var updateSuccess = await _workItemStore.UpdateAsync(evt.WorkItemId, updatedWorkItem, ct);

                if (!updateSuccess)
                {
                    _logger.LogWarning("Work item {WorkItemId} update failed", evt.WorkItemId);
                    await _notificationHub.WriteAsync(new Notification(
                        Type: "PROCESSING_ERROR",
                        TransactionId: transactionId,
                        EncounterId: evt.EncounterId,
                        PatientId: evt.PatientId,
                        Message: "Work item update failed"), ct);
                    return;
                }

                _logger.LogInformation(
                    "Updated work item {WorkItemId} to status {Status} with ServiceRequestId {ServiceRequestId} and ProcedureCode {ProcedureCode}",
                    evt.WorkItemId,
                    status,
                    serviceRequestId,
                    formData.ProcedureCode);
            }
            else
            {
                // Fallback to just updating status if work item not found (shouldn't happen in normal flow)
                var statusUpdateSuccess = await _workItemStore.UpdateStatusAsync(evt.WorkItemId, status, ct);

                if (!statusUpdateSuccess)
                {
                    _logger.LogWarning("Work item {WorkItemId} status update failed", evt.WorkItemId);
                    await _notificationHub.WriteAsync(new Notification(
                        Type: "PROCESSING_ERROR",
                        TransactionId: transactionId,
                        EncounterId: evt.EncounterId,
                        PatientId: evt.PatientId,
                        Message: "Work item status update failed"), ct);
                    return;
                }

                _logger.LogWarning(
                    "Work item {WorkItemId} not found for full update, updated status only to {Status}",
                    evt.WorkItemId,
                    status);
            }

            // Step 4b: Send WORK_ITEM_STATUS_CHANGED notification
            var statusChangeNotification = new Notification(
                Type: "WORK_ITEM_STATUS_CHANGED",
                TransactionId: transactionId,
                EncounterId: evt.EncounterId,
                PatientId: evt.PatientId,
                Message: $"Work item {evt.WorkItemId} status changed to {status}",
                WorkItemId: evt.WorkItemId,
                NewStatus: status.ToString(),
                ServiceRequestId: serviceRequestId,
                ProcedureCode: formData.ProcedureCode);
            await _notificationHub.WriteAsync(statusChangeNotification, ct);

            _logger.LogInformation(
                "Sent WORK_ITEM_STATUS_CHANGED notification for work item {WorkItemId}",
                evt.WorkItemId);

            // Step 5: Generate PDF from form data
            var pdfBytes = await _pdfStamper.StampFormAsync(formData, ct);

            _logger.LogInformation(
                "Generated PDF form: {Size} bytes for encounter {EncounterId}",
                pdfBytes.Length,
                evt.EncounterId);

            // Step 6: Store formData and PDF in result store with transaction ID
            var cacheKey = $"{evt.EncounterId}:{transactionId}";
            await _resultStore.SetCachedResponseAsync(cacheKey, formData, ct);
            await _resultStore.SetCachedPdfAsync(cacheKey, pdfBytes, ct);

            _logger.LogInformation(
                "Stored PDF with cache key {CacheKey}",
                cacheKey);

            // Step 7: Notify subscribers that PA form is ready
            var notification = new Notification(
                Type: "PA_FORM_READY",
                TransactionId: transactionId,
                EncounterId: evt.EncounterId,
                PatientId: evt.PatientId,
                Message: $"Prior authorization form ready. Recommendation: {formData.Recommendation}"
            );

            await _notificationHub.WriteAsync(notification, ct);

            _logger.LogInformation(
                "Notification sent for encounter {EncounterId}: {Type}",
                evt.EncounterId,
                notification.Type);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Service error for encounter {EncounterId}: {Message}",
                evt.EncounterId,
                ex.Message);

            // Notify subscribers of the processing error
            await _notificationHub.WriteAsync(new Notification(
                Type: "PROCESSING_ERROR",
                TransactionId: transactionId,
                EncounterId: evt.EncounterId,
                PatientId: evt.PatientId,
                Message: $"Service error: {ex.Message}"), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing encounter {EncounterId}: {Message}",
                evt.EncounterId,
                ex.Message);

            // Notify subscribers of the processing error (no sensitive stack trace)
            await _notificationHub.WriteAsync(new Notification(
                Type: "PROCESSING_ERROR",
                TransactionId: transactionId,
                EncounterId: evt.EncounterId,
                PatientId: evt.PatientId,
                Message: "Unexpected processing error"), ct);
        }
    }

    /// <inheritdoc />
    [Obsolete("Use ProcessAsync(EncounterCompletedEvent, CancellationToken) instead.")]
    public async Task ProcessEncounterAsync(string encounterId, string patientId, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing encounter {EncounterId} for patient {PatientId}",
            encounterId,
            patientId);

        var transactionId = $"pa-{encounterId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        try
        {
            // Step 1: Hydrate clinical context via FHIR data aggregator
            var clinicalBundle = await _aggregator.AggregateClinicalDataAsync(
                patientId,
                encounterId,
                ct);

            _logger.LogInformation(
                "Hydrated clinical bundle with {Conditions} conditions, {Observations} observations, {Procedures} procedures",
                clinicalBundle.Conditions.Count,
                clinicalBundle.Observations.Count,
                clinicalBundle.Procedures.Count);

            // Step 2: Extract procedure code from ServiceRequest (fall back to default)
            var procedureCode = clinicalBundle.ServiceRequests
                .FirstOrDefault(sr => sr.Status == "active")
                ?.Code?.Coding?.FirstOrDefault()?.Code
                ?? DefaultProcedureCode;

            // Step 3: Send to Intelligence service for PA analysis
            var formData = await _intelligenceClient.AnalyzeAsync(
                clinicalBundle,
                procedureCode,
                ct);

            _logger.LogInformation(
                "Intelligence analysis complete. Recommendation: {Recommendation}, Confidence: {Confidence:P0}",
                formData.Recommendation,
                formData.ConfidenceScore);

            // Step 4: Generate PDF from form data
            var pdfBytes = await _pdfStamper.StampFormAsync(formData, ct);

            _logger.LogInformation(
                "Generated PDF form: {Size} bytes for encounter {EncounterId}",
                pdfBytes.Length,
                encounterId);

            // Step 4: Store formData and PDF in result store with transaction ID
            var cacheKey = $"{encounterId}:{transactionId}";
            await _resultStore.SetCachedResponseAsync(cacheKey, formData, ct);
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

            // Notify subscribers of the processing error
            await _notificationHub.WriteAsync(new Notification(
                Type: "PROCESSING_ERROR",
                TransactionId: transactionId,
                EncounterId: encounterId,
                PatientId: patientId,
                Message: $"Service error: {ex.Message}"), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing encounter {EncounterId}: {Message}",
                encounterId,
                ex.Message);

            // Notify subscribers of the processing error (no sensitive stack trace)
            await _notificationHub.WriteAsync(new Notification(
                Type: "PROCESSING_ERROR",
                TransactionId: transactionId,
                EncounterId: encounterId,
                PatientId: patientId,
                Message: "Unexpected processing error"), ct);
        }
    }
}
