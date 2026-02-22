using Gateway.API.GraphQL.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// Store for prior authorization request persistence and retrieval.
/// </summary>
public interface IPARequestStore
{
    /// <summary>
    /// Gets all PA requests ordered by creation date descending.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All PA requests.</returns>
    Task<IReadOnlyList<PARequestModel>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a single PA request by ID.
    /// </summary>
    /// <param name="id">The PA request ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The PA request if found, null otherwise.</returns>
    Task<PARequestModel?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new PA request. Returns the created model with generated ID.
    /// </summary>
    /// <param name="request">The PA request to create.</param>
    /// <param name="fhirPatientId">The FHIR patient ID to associate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created PA request model.</returns>
    Task<PARequestModel> CreateAsync(PARequestModel request, string fhirPatientId, CancellationToken ct = default);

    /// <summary>
    /// Updates editable fields on a PA request.
    /// </summary>
    /// <param name="id">The PA request ID.</param>
    /// <param name="diagnosis">Updated diagnosis text.</param>
    /// <param name="diagnosisCode">Updated diagnosis code.</param>
    /// <param name="serviceDate">Updated service date.</param>
    /// <param name="placeOfService">Updated place of service.</param>
    /// <param name="clinicalSummary">Updated clinical summary.</param>
    /// <param name="criteria">Updated criteria list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated PA request if found, null otherwise.</returns>
    Task<PARequestModel?> UpdateFieldsAsync(
        string id,
        string? diagnosis,
        string? diagnosisCode,
        string? serviceDate,
        string? placeOfService,
        string? clinicalSummary,
        IReadOnlyList<CriterionModel>? criteria,
        CancellationToken ct = default);

    /// <summary>
    /// Applies AI analysis results. Sets status to "ready".
    /// </summary>
    /// <param name="id">The PA request ID.</param>
    /// <param name="clinicalSummary">AI-generated clinical summary.</param>
    /// <param name="confidence">Confidence score from analysis.</param>
    /// <param name="criteria">AI-generated criteria list.</param>
    /// <param name="diagnosisCode">Auto-detected diagnosis code (optional).</param>
    /// <param name="diagnosisName">Auto-detected diagnosis name (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated PA request if found, null otherwise.</returns>
    Task<PARequestModel?> ApplyAnalysisResultAsync(
        string id,
        string clinicalSummary,
        int confidence,
        IReadOnlyList<CriterionModel> criteria,
        string? diagnosisCode = null,
        string? diagnosisName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Submits PA request for insurance review. Sets status to "waiting_for_insurance".
    /// </summary>
    /// <param name="id">The PA request ID.</param>
    /// <param name="addReviewTimeSeconds">Additional review time to record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated PA request if found, null otherwise.</returns>
    Task<PARequestModel?> SubmitAsync(string id, int addReviewTimeSeconds = 0, CancellationToken ct = default);

    /// <summary>
    /// Adds review time to a PA request.
    /// </summary>
    /// <param name="id">The PA request ID.</param>
    /// <param name="seconds">Number of seconds to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated PA request if found, null otherwise.</returns>
    Task<PARequestModel?> AddReviewTimeAsync(string id, int seconds, CancellationToken ct = default);

    /// <summary>
    /// Deletes a PA request. Returns true if found and deleted.
    /// </summary>
    /// <param name="id">The PA request ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if found and deleted, false otherwise.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Gets aggregated PA request statistics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The PA statistics model.</returns>
    Task<PAStatsModel> GetStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets recent activity (last 5 updates).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of recent activity items.</returns>
    Task<IReadOnlyList<ActivityItemModel>> GetActivityAsync(CancellationToken ct = default);
}
