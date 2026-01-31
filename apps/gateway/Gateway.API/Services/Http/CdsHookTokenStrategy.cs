using Gateway.API.Contracts.Http;
using Microsoft.AspNetCore.Http;

namespace Gateway.API.Services.Http;

/// <summary>
/// Token strategy that extracts the access token from CDS Hook request context.
/// </summary>
public sealed class CdsHookTokenStrategy : ITokenAcquisitionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="CdsHookTokenStrategy"/>.
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor for reading request context.</param>
    public CdsHookTokenStrategy(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public bool CanHandle =>
        _httpContextAccessor.HttpContext?.Items.ContainsKey("FhirAccessToken") == true;

    /// <inheritdoc />
    public Task<string?> AcquireTokenAsync(CancellationToken ct = default)
    {
        var token = _httpContextAccessor.HttpContext?.Items["FhirAccessToken"] as string;
        return Task.FromResult(token);
    }
}
