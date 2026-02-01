using Gateway.API.Contracts;

namespace Gateway.API.Filters;

/// <summary>
/// Endpoint filter that validates API keys from the X-API-Key header.
/// </summary>
public sealed class ApiKeyEndpointFilter : IEndpointFilter
{
    private const string ApiKeyHeader = "X-API-Key";
    private readonly IApiKeyValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyEndpointFilter"/> class.
    /// </summary>
    /// <param name="validator">The API key validator.</param>
    public ApiKeyEndpointFilter(IApiKeyValidator validator)
    {
        _validator = validator;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var apiKey = context.HttpContext.Request.Headers[ApiKeyHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Results.Problem(
                detail: "API key is required. Include the X-API-Key header.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
        }

        if (!_validator.IsValid(apiKey))
        {
            return Results.Problem(
                detail: "Invalid API key.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
        }

        return await next(context);
    }
}
