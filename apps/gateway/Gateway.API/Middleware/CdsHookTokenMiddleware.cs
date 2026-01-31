using System.Text.Json;

namespace Gateway.API.Middleware;

/// <summary>
/// Middleware that extracts FHIR access tokens from CDS Hook request payloads.
/// </summary>
public sealed class CdsHookTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CdsHookTokenMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdsHookTokenMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public CdsHookTokenMiddleware(RequestDelegate next, ILogger<CdsHookTokenMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only process CDS Hooks requests
        if (context.Request.Path.StartsWithSegments("/cds-hooks") &&
            context.Request.Method == HttpMethods.Post)
        {
            await ExtractAndStoreFhirTokenAsync(context);
        }

        await _next(context);
    }

    private async Task ExtractAndStoreFhirTokenAsync(HttpContext context)
    {
        try
        {
            // Enable buffering so the body can be read multiple times
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset for downstream handlers

            if (string.IsNullOrEmpty(body))
            {
                return;
            }

            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("fhirAuthorization", out var fhirAuth) &&
                fhirAuth.TryGetProperty("access_token", out var tokenElement))
            {
                var token = tokenElement.GetString();
                if (!string.IsNullOrEmpty(token))
                {
                    context.Items["FhirAccessToken"] = token;
                    _logger.LogDebug("Extracted FHIR access token from CDS Hook request");
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse CDS Hook request body for token extraction");
        }
    }
}
