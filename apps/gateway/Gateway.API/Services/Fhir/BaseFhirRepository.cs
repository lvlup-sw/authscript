using Gateway.API.Abstractions;
using Gateway.API.Contracts.Fhir;
using Hl7.Fhir.Model;

namespace Gateway.API.Services.Fhir;

/// <summary>
/// Base repository implementation that wraps an IFhirContext.
/// Provides common repository functionality for FHIR resources.
/// </summary>
/// <typeparam name="TResource">The FHIR resource type.</typeparam>
public abstract class BaseFhirRepository<TResource> : IFhirRepository<TResource> where TResource : Resource
{
    /// <summary>
    /// The underlying FHIR context.
    /// </summary>
    protected readonly IFhirContext<TResource> Context;

    /// <summary>
    /// Logger for diagnostic output.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseFhirRepository{TResource}"/> class.
    /// </summary>
    /// <param name="context">The FHIR context.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    protected BaseFhirRepository(IFhirContext<TResource> context, ILogger logger)
    {
        Context = context;
        Logger = logger;
    }

    /// <inheritdoc />
    public virtual Task<Result<TResource>> GetByIdAsync(
        string id,
        string accessToken,
        CancellationToken ct = default)
    {
        return Context.ReadAsync(id, accessToken, ct);
    }

    /// <inheritdoc />
    public virtual Task<Result<IReadOnlyList<TResource>>> FindByPatientAsync(
        string patientId,
        string accessToken,
        CancellationToken ct = default)
    {
        var query = $"patient={patientId}";
        return Context.SearchAsync(query, accessToken, ct);
    }

    /// <summary>
    /// Builds a search query string from parameters.
    /// </summary>
    /// <param name="parameters">Query parameters as key-value pairs.</param>
    /// <returns>The formatted query string.</returns>
    protected static string BuildQuery(params (string key, string value)[] parameters)
    {
        return string.Join("&", parameters.Select(p => $"{p.key}={Uri.EscapeDataString(p.value)}"));
    }
}

/// <summary>
/// Base repository with date range support.
/// </summary>
/// <typeparam name="TResource">The FHIR resource type.</typeparam>
public abstract class BaseFhirRepositoryWithDateRange<TResource>
    : BaseFhirRepository<TResource>, IFhirRepositoryWithDateRange<TResource>
    where TResource : Resource
{
    /// <summary>
    /// The name of the date field to filter on.
    /// </summary>
    protected abstract string DateFieldName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseFhirRepositoryWithDateRange{TResource}"/> class.
    /// </summary>
    /// <param name="context">The FHIR context.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    protected BaseFhirRepositoryWithDateRange(IFhirContext<TResource> context, ILogger logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc />
    public virtual Task<Result<IReadOnlyList<TResource>>> FindByPatientSinceAsync(
        string patientId,
        DateOnly since,
        string accessToken,
        CancellationToken ct = default)
    {
        var query = BuildQuery(
            ("patient", patientId),
            (DateFieldName, $"ge{since:yyyy-MM-dd}"));

        return Context.SearchAsync(query, accessToken, ct);
    }
}
