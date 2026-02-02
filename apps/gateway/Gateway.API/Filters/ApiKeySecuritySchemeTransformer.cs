using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Gateway.API.Filters;

/// <summary>
/// OpenAPI document transformer that adds the X-API-Key security scheme.
/// </summary>
public sealed class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SecuritySchemeName = "ApiKey";

    /// <inheritdoc />
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            [SecuritySchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = "X-API-Key",
                In = ParameterLocation.Header,
                Description = "API key required for protected endpoints"
            }
        };

        document.Components ??= new OpenApiComponents();

        // Add API key scheme without overwriting existing schemes
        if (document.Components.SecuritySchemes is null)
        {
            document.Components.SecuritySchemes = securitySchemes;
        }
        else
        {
            foreach (var scheme in securitySchemes)
            {
                document.Components.SecuritySchemes.TryAdd(scheme.Key, scheme.Value);
            }
        }

        // Apply security requirement to all operations
        var operations = document.Paths?.Values
            .Where(path => path.Operations is not null)
            .SelectMany(path => path.Operations!);

        if (operations is null) return Task.CompletedTask;

        foreach (var operation in operations)
        {
            operation.Value.Security ??= [];
            operation.Value.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SecuritySchemeName, document)] = []
            });
        }

        return Task.CompletedTask;
    }
}
