using Gateway.API.Models;

namespace Gateway.API.Contracts;

/// <summary>
/// Processes finished encounters to generate prior authorization forms.
/// </summary>
public interface IEncounterProcessor
{
    /// <summary>
    /// Processes an encounter completion event by hydrating clinical context,
    /// generating PA form, and updating the work item status.
    /// </summary>
    /// <param name="evt">The encounter completed event with full context.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ProcessAsync(EncounterCompletedEvent evt, CancellationToken ct);

    /// <summary>
    /// Processes an encounter by hydrating clinical context and generating PA form.
    /// </summary>
    /// <param name="encounterId">The FHIR Encounter resource ID.</param>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="ct">Cancellation token.</param>
    [Obsolete("Use ProcessAsync(EncounterCompletedEvent, CancellationToken) instead.")]
    Task ProcessEncounterAsync(string encounterId, string patientId, CancellationToken ct);
}
