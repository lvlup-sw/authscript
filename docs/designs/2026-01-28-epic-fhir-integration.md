# Design: Epic FHIR Integration with Dual Authentication

## Problem Statement

The Gateway needs to authenticate with Epic's FHIR R4 API using two distinct flows:

1. **CDS Hooks**: Epic provides an `access_token` in the request payload (`fhirAuthorization.access_token`)
2. **Backend Services**: Server-to-server JWT-based client credentials flow

The current implementation threads `accessToken` through every method call, requiring callers to manage token acquisition. Additionally, the repository inheritance hierarchy violates the depth limit (>2 levels) and should be flattened using composition.

## Chosen Approach

**Strategy Pattern + Composition** following these principles:

| Principle | Implementation |
|-----------|----------------|
| OCP | Add auth methods via new strategies, not switches |
| ISP | Small interfaces: `ITokenAcquisitionStrategy`, `IFhirHttpClientProvider` |
| DIP | Depend on abstractions, inject via constructor |
| Composition | Sealed classes, no inheritance chains |

## Technical Design

### Authentication Layer

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Token Acquisition Strategies                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   ┌─────────────────────────┐      ┌─────────────────────────────────┐  │
│   │  CdsHookTokenStrategy   │      │   JwtBackendTokenStrategy       │  │
│   │  ─────────────────────  │      │   ─────────────────────────     │  │
│   │  • Reads HttpContext    │      │   • Generates JWT assertion     │  │
│   │  • Extracts fhirAuth    │      │   • POST to token endpoint      │  │
│   │  • Zero network calls   │      │   • Caches until expiry (~59m)  │  │
│   └─────────────────────────┘      └─────────────────────────────────┘  │
│              │                                    │                      │
└──────────────┼────────────────────────────────────┼──────────────────────┘
               │                                    │
               ▼                                    ▼
       ┌─────────────────────────────────────────────────────┐
       │              ITokenStrategyResolver                  │
       │  ─────────────────────────────────────────────────  │
       │  • Inspects IHttpContextAccessor                     │
       │  • CDS token present? → CdsHookTokenStrategy         │
       │  • Otherwise → JwtBackendTokenStrategy               │
       └─────────────────────────────────────────────────────┘
                              │
                              ▼
       ┌─────────────────────────────────────────────────────┐
       │            IFhirHttpClientProvider                   │
       │  ─────────────────────────────────────────────────  │
       │  • Uses IHttpClientFactory for lifecycle             │
       │  • Calls resolver.GetStrategy().AcquireTokenAsync()  │
       │  • Attaches Bearer token to HttpClient               │
       │  • Returns ready-to-use HttpClient                   │
       └─────────────────────────────────────────────────────┘
```

### Interfaces

```csharp
/// <summary>
/// Strategy for acquiring OAuth bearer tokens.
/// </summary>
public interface ITokenAcquisitionStrategy
{
    /// <summary>
    /// Acquires a bearer token for Epic FHIR API access.
    /// </summary>
    Task<string?> AcquireTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Indicates if this strategy can provide a token in the current context.
    /// </summary>
    bool CanHandle { get; }
}

/// <summary>
/// Resolves the appropriate token acquisition strategy for the current request.
/// </summary>
public interface ITokenStrategyResolver
{
    /// <summary>
    /// Gets the token acquisition strategy for the current context.
    /// </summary>
    ITokenAcquisitionStrategy Resolve();
}

/// <summary>
/// Provides authenticated HTTP clients for Epic FHIR API.
/// </summary>
public interface IFhirHttpClientProvider
{
    /// <summary>
    /// Gets an HTTP client with Bearer token attached.
    /// </summary>
    Task<HttpClient> GetAuthenticatedClientAsync(CancellationToken ct = default);
}
```

### Strategy Implementations

#### CdsHookTokenStrategy

Extracts token from CDS Hook request context:

```csharp
public sealed class CdsHookTokenStrategy : ITokenAcquisitionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public bool CanHandle =>
        _httpContextAccessor.HttpContext?.Items.ContainsKey("FhirAccessToken") == true;

    public Task<string?> AcquireTokenAsync(CancellationToken ct = default)
    {
        var token = _httpContextAccessor.HttpContext?.Items["FhirAccessToken"] as string;
        return Task.FromResult(token);
    }
}
```

Note: CDS Hook middleware populates `HttpContext.Items["FhirAccessToken"]` from the request.

#### JwtBackendTokenStrategy

Implements Epic Backend Services flow:

```csharp
public sealed class JwtBackendTokenStrategy : ITokenAcquisitionStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EpicAuthOptions _options;
    private readonly IMemoryCache _tokenCache;

    public bool CanHandle => true; // Fallback strategy

    public async Task<string?> AcquireTokenAsync(CancellationToken ct = default)
    {
        // Check cache first
        if (_tokenCache.TryGetValue("EpicAccessToken", out string? cached))
            return cached;

        // Generate JWT assertion
        var jwt = GenerateClientAssertion();

        // Exchange for access token
        var token = await ExchangeJwtForTokenAsync(jwt, ct);

        // Cache for ~55 minutes (tokens valid for 60)
        _tokenCache.Set("EpicAccessToken", token, TimeSpan.FromMinutes(55));

        return token;
    }

    private string GenerateClientAssertion()
    {
        // JWT with claims: iss, sub (both = ClientId), aud, jti, exp
        // Signed with RS384 using private key
    }
}
```

### FHIR Client Layer (Flattened)

Remove inheritance chain. Use composition with sealed classes:

```
┌──────────────────────────────────────────────────────────────┐
│                    IFhirHttpClient (Revised)                  │
│  ─────────────────────────────────────────────────────────   │
│  • ReadAsync(resourceType, id)          // No accessToken    │
│  • SearchAsync(resourceType, query)     // Provider handles  │
│  • CreateAsync(resourceType, resource)  // auth internally   │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│            FhirHttpClient (sealed, composition)               │
│  ─────────────────────────────────────────────────────────   │
│  private readonly IFhirHttpClientProvider _clientProvider;   │
│                                                               │
│  public async Task<Result<JsonElement>> ReadAsync(...)       │
│  {                                                            │
│      var client = await _clientProvider                       │
│          .GetAuthenticatedClientAsync(ct);                    │
│      // Use client for request                                │
│  }                                                            │
└──────────────────────────────────────────────────────────────┘
```

### Repository Layer (Thin Facades)

For PoC, keep repositories as thin domain facades:

```csharp
/// <summary>
/// Repository for Condition resources.
/// </summary>
public sealed class ConditionRepository
{
    private readonly IFhirHttpClient _client;
    private readonly ILogger<ConditionRepository> _logger;

    /// <summary>
    /// Finds active conditions for a patient.
    /// </summary>
    public async Task<Result<IReadOnlyList<ConditionInfo>>> FindActiveAsync(
        string patientId,
        CancellationToken ct = default)
    {
        var result = await _client.SearchAsync(
            "Condition",
            $"patient={patientId}&clinical-status=active",
            ct);

        return result.Match(
            json => Result<IReadOnlyList<ConditionInfo>>.Success(MapConditions(json)),
            error => Result<IReadOnlyList<ConditionInfo>>.Failure(error));
    }
}
```

Benefits:
- **Domain language**: `FindActiveAsync` vs generic `SearchAsync`
- **Mapping logic encapsulated**: JSON → ConditionInfo
- **Sealed**: No inheritance, composition only
- **Removable**: If we pivot, repositories can be eliminated

### Configuration

```csharp
public sealed record EpicAuthOptions
{
    public const string SectionName = "Epic";

    public required string ClientId { get; init; }
    public required string TokenEndpoint { get; init; }
    public required string FhirBaseUrl { get; init; }
    public required string PrivateKeyPath { get; init; }
    public string SigningAlgorithm { get; init; } = "RS384";
}
```

### DI Registration

```csharp
public static IServiceCollection AddEpicFhirServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Configuration
    services.AddOptions<EpicAuthOptions>()
        .Bind(configuration.GetSection(EpicAuthOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Token strategies
    services.AddScoped<CdsHookTokenStrategy>();
    services.AddScoped<JwtBackendTokenStrategy>();
    services.AddScoped<ITokenStrategyResolver, TokenStrategyResolver>();

    // HTTP client with factory
    services.AddHttpClient("EpicFhir", client =>
    {
        var options = configuration
            .GetSection(EpicAuthOptions.SectionName)
            .Get<EpicAuthOptions>()!;
        client.BaseAddress = new Uri(options.FhirBaseUrl);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/fhir+json"));
    });

    // Providers and clients
    services.AddScoped<IFhirHttpClientProvider, FhirHttpClientProvider>();
    services.AddScoped<IFhirHttpClient, FhirHttpClient>();

    // Repositories (optional thin facades)
    services.AddScoped<ConditionRepository>();
    services.AddScoped<ObservationRepository>();

    return services;
}
```

## Integration Points

### Existing Code Migration

| Current | Migration |
|---------|-----------|
| `IFhirHttpClient.ReadAsync(..., accessToken)` | Remove `accessToken` param |
| `BaseFhirRepository<T>` | Delete, replace with sealed classes |
| `BaseFhirRepositoryWithDateRange<T>` | Delete, use composition |
| `EpicFhirContext<T>` | Refactor to use `IFhirHttpClientProvider` |
| `FhirClient` | Refactor to use new interfaces |

### CDS Hook Middleware

Add middleware to extract token from CDS Hook payload:

```csharp
public sealed class CdsHookTokenMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/cds-hooks"))
        {
            // Parse request body for fhirAuthorization.access_token
            var token = await ExtractFhirTokenAsync(context.Request);
            if (token is not null)
            {
                context.Items["FhirAccessToken"] = token;
            }
        }

        await next(context);
    }
}
```

## Testing Strategy

### Unit Tests

| Component | Test Focus |
|-----------|------------|
| `CdsHookTokenStrategy` | Extracts token from HttpContext.Items |
| `JwtBackendTokenStrategy` | JWT generation, token caching |
| `TokenStrategyResolver` | Correct strategy selection |
| `FhirHttpClientProvider` | Token attachment, factory usage |
| `ConditionRepository` | JSON mapping, query building |

### Integration Tests

| Scenario | Verification |
|----------|--------------|
| CDS Hook flow | Token passed through correctly |
| Backend flow | JWT exchange, token caching |
| Epic Sandbox | End-to-end with test patients |

## Open Questions

1. **Private key storage**: Azure Key Vault vs file system vs environment variable?
   - *Recommendation*: Environment variable for PoC, Key Vault for production

2. **Token refresh strategy**: Proactive vs on-demand?
   - *Recommendation*: On-demand with 55-minute cache (tokens valid 60 min)

3. **Rate limiting**: Does Epic enforce limits we need to handle?
   - *Action*: Verify in Epic documentation before implementation

## File Structure

```
Gateway.API/
├── Contracts/
│   └── Http/
│       ├── ITokenAcquisitionStrategy.cs
│       ├── ITokenStrategyResolver.cs
│       └── IFhirHttpClientProvider.cs
├── Configuration/
│   └── EpicAuthOptions.cs
├── Services/
│   └── Http/
│       ├── CdsHookTokenStrategy.cs
│       ├── JwtBackendTokenStrategy.cs
│       ├── TokenStrategyResolver.cs
│       └── FhirHttpClientProvider.cs
├── Middleware/
│   └── CdsHookTokenMiddleware.cs
└── DependencyExtensions.cs (updated)
```

## Implementation Order

1. **Contracts**: Define new interfaces (breaking change to existing)
2. **Configuration**: Add `EpicAuthOptions` record
3. **Token Strategies**: Implement both strategies
4. **Resolver**: Implement strategy selection
5. **Provider**: Implement `FhirHttpClientProvider`
6. **Refactor FhirHttpClient**: Remove `accessToken`, use provider
7. **Refactor Repositories**: Flatten to sealed classes
8. **Middleware**: Add CDS token extraction
9. **DI Registration**: Wire up new services
10. **Tests**: Unit tests for each component
