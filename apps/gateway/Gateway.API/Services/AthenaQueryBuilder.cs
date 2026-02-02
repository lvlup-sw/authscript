// =============================================================================
// <copyright file="AthenaQueryBuilder.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Services;

/// <summary>
/// Provides utility methods for building athenahealth-specific FHIR query parameters.
/// </summary>
public static class AthenaQueryBuilder
{
    /// <summary>
    /// Builds a query string for fetching encounter data from athenahealth's FHIR API.
    /// </summary>
    /// <param name="patientId">The FHIR patient resource ID.</param>
    /// <param name="encounterId">The FHIR encounter resource ID.</param>
    /// <param name="practiceId">The athenahealth practice ID.</param>
    /// <returns>
    /// A formatted query string including the patient, encounter ID, and the
    /// athenahealth-specific ah-practice parameter.
    /// </returns>
    /// <remarks>
    /// The <c>ah-practice</c> parameter is required by athenahealth's FHIR API to scope
    /// queries to a specific practice. The format is:
    /// <c>Organization/a-1.Practice-{practiceId}</c>.
    /// </remarks>
    public static string BuildEncounterQuery(string patientId, string encounterId, string practiceId)
        => $"patient={patientId}&_id={encounterId}&ah-practice=Organization/a-1.Practice-{practiceId}";
}
