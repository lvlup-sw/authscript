namespace Gateway.API.Contracts;

/// <summary>
/// Processes finished encounters to generate prior authorization forms.
/// </summary>
public interface IEncounterProcessor
{
    /// <summary>
    /// Processes an encounter by hydrating clinical context and generating PA form.
    /// </summary>
    /// <param name="encounterId">The FHIR Encounter resource ID.</param>
    /// <param name="patientId">The FHIR Patient resource ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ProcessEncounterAsync(string encounterId, string patientId, CancellationToken ct);
}
