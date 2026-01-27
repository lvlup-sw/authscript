# Gateway.API FHIR Infrastructure Refactor

**Date:** 2026-01-26
**Status:** Draft
**Reference:** aegis-api patterns from ares-elite-platform

## Overview

Refactor Gateway.API to adopt proven patterns from aegis-api, establishing a stable foundation for the prior authorization demo. This includes standardized error handling, proper FHIR serialization, centralized HTTP client management with resilience, and improved file organization.

## Goals

1. **Standardize error handling** using `Result<T>` pattern across all services
2. **Proper FHIR serialization** using `Hl7.Fhir.Serialization` library
3. **Centralized HTTP client** with `IHttpClientProvider` and resilience policies
4. **Strongly-typed configuration** via Options pattern
5. **Single public type per file** compliance with C# coding standards

## Non-Goals

- Multi-project architecture (Core/Infrastructure separation)
- On-Behalf-Of (OBO) authentication flow (client credentials only)
- Full BaseFhirService/BaseFhirRepository hierarchy (keep current simpler structure)
- CDS Hooks refactoring (out of scope)

---

## Architecture

### Directory Structure (After)

```
Gateway.API/
├── Abstractions/
│   ├── Result.cs              # Result<T> generic type
│   ├── Error.cs               # Error record with Code, Message, Type
│   ├── ErrorType.cs           # Enum mapping to HTTP status codes
│   └── ErrorFactory.cs        # Static factory for common errors
├── Configuration/
│   ├── EpicFhirOptions.cs     # FHIR endpoint configuration
│   ├── IntelligenceOptions.cs # Intelligence service config
│   └── ResiliencyOptions.cs   # Retry, timeout, circuit breaker settings
├── Contracts/
│   ├── Fhir/
│   │   ├── IFhirContext.cs    # (existing, unchanged)
│   │   ├── IFhirRepository.cs # (existing, unchanged)
│   │   └── IFhirSerializer.cs # NEW: serialization abstraction
│   ├── Http/
│   │   └── IHttpClientProvider.cs  # NEW: authenticated client provider
│   ├── IEpicFhirClient.cs     # UPDATE: return Result<T>
│   ├── IFhirDataAggregator.cs # UPDATE: return Result<T>
│   ├── IIntelligenceClient.cs # UPDATE: return Result<T>
│   └── IEpicUploader.cs       # UPDATE: return Result<T>
├── Errors/
│   └── FhirErrors.cs          # FHIR-specific error definitions
├── Services/
│   ├── Fhir/
│   │   ├── EpicFhirContext.cs # UPDATE: use IFhirSerializer
│   │   ├── FhirSerializer.cs  # NEW: Hl7.Fhir.Serialization wrapper
│   │   └── ...repositories    # (existing, unchanged)
│   ├── Http/
│   │   └── HttpClientProvider.cs  # NEW: token acquisition + client creation
│   ├── EpicFhirClient.cs      # UPDATE: use Result<T>, IHttpClientProvider
│   ├── FhirDataAggregator.cs  # UPDATE: use Result<T>
│   ├── IntelligenceClient.cs  # UPDATE: use Result<T>
│   └── EpicUploader.cs        # UPDATE: use Result<T>, IHttpClientProvider
├── Extensions/
│   └── ServiceCollectionExtensions.cs  # DI registration helpers
└── Program.cs                 # UPDATE: use extensions for clean registration
```

---

## Component Designs

### 1. Abstractions Layer

#### Result.cs

Lift verbatim from aegis-api with minor adjustments for our namespace.

```csharp
namespace Gateway.API.Abstractions;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly record struct Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;

    private Result(T value) { Value = value; Error = null; }
    private Result(Error error) { Value = default; Error = error; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        => IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
```

#### Error.cs

```csharp
namespace Gateway.API.Abstractions;

/// <summary>
/// Represents an error from an operation.
/// </summary>
/// <param name="Code">Machine-readable error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Type">Error classification for HTTP status mapping.</param>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Unexpected)
{
    /// <summary>Optional inner exception for logging.</summary>
    public Exception? Inner { get; init; }
}
```

#### ErrorType.cs

```csharp
namespace Gateway.API.Abstractions;

/// <summary>
/// Error classification mapped to HTTP status codes.
/// </summary>
public enum ErrorType
{
    None = 0,
    NotFound = 404,
    Validation = 400,
    Conflict = 409,
    Unauthorized = 401,
    Forbidden = 403,
    Infrastructure = 503,
    Unexpected = 500
}
```

#### ErrorFactory.cs

```csharp
namespace Gateway.API.Abstractions;

/// <summary>
/// Factory methods for common error types.
/// </summary>
public static class ErrorFactory
{
    public static Error NotFound(string resource, string id)
        => new($"{resource}.NotFound", $"{resource}/{id} not found", ErrorType.NotFound);

    public static Error Validation(string message)
        => new("Validation.Failed", message, ErrorType.Validation);

    public static Error Unauthorized(string message = "Authentication required")
        => new("Auth.Unauthorized", message, ErrorType.Unauthorized);

    public static Error Infrastructure(string message, Exception? inner = null)
        => new("Infrastructure.Error", message, ErrorType.Infrastructure) { Inner = inner };

    public static Error Unexpected(string message, Exception? inner = null)
        => new("Unexpected.Error", message, ErrorType.Unexpected) { Inner = inner };
}
```

---

### 2. Configuration Layer

#### EpicFhirOptions.cs

```csharp
namespace Gateway.API.Configuration;

/// <summary>
/// Configuration for Epic FHIR API connectivity.
/// </summary>
public sealed class EpicFhirOptions
{
    public const string SectionName = "Epic";

    /// <summary>Base URL for Epic FHIR R4 API.</summary>
    public required string FhirBaseUrl { get; init; }

    /// <summary>OAuth client ID for Epic.</summary>
    public required string ClientId { get; init; }

    /// <summary>OAuth client secret (from user-secrets in dev).</summary>
    public string? ClientSecret { get; init; }

    /// <summary>Token endpoint for client credentials flow.</summary>
    public string? TokenEndpoint { get; init; }
}
```

#### IntelligenceOptions.cs

```csharp
namespace Gateway.API.Configuration;

/// <summary>
/// Configuration for Intelligence service connectivity.
/// </summary>
public sealed class IntelligenceOptions
{
    public const string SectionName = "Intelligence";

    /// <summary>Base URL for Intelligence API.</summary>
    public required string BaseUrl { get; init; }

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 30;
}
```

#### ResiliencyOptions.cs

```csharp
namespace Gateway.API.Configuration;

/// <summary>
/// Configuration for HTTP resilience policies.
/// </summary>
public sealed class ResiliencyOptions
{
    public const string SectionName = "Resilience";

    /// <summary>Maximum retry attempts.</summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>Base delay between retries in seconds.</summary>
    public double RetryDelaySeconds { get; init; } = 1.0;

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>Circuit breaker failure threshold.</summary>
    public int CircuitBreakerThreshold { get; init; } = 5;

    /// <summary>Circuit breaker break duration in seconds.</summary>
    public int CircuitBreakerDurationSeconds { get; init; } = 30;
}
```

---

### 3. FHIR Serialization

#### IFhirSerializer.cs

```csharp
namespace Gateway.API.Contracts.Fhir;

using Hl7.Fhir.Model;

/// <summary>
/// Abstraction for FHIR JSON serialization.
/// </summary>
public interface IFhirSerializer
{
    /// <summary>Serialize a FHIR resource to JSON string.</summary>
    string Serialize<T>(T resource) where T : Resource;

    /// <summary>Deserialize JSON string to FHIR resource.</summary>
    T? Deserialize<T>(string json) where T : Resource;

    /// <summary>Deserialize JSON to a Bundle resource.</summary>
    Bundle? DeserializeBundle(string json);
}
```

#### FhirSerializer.cs

```csharp
namespace Gateway.API.Services.Fhir;

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Gateway.API.Contracts.Fhir;

/// <summary>
/// FHIR JSON serialization using Hl7.Fhir library.
/// </summary>
public sealed class FhirSerializer : IFhirSerializer
{
    private static readonly FhirJsonSerializer s_serializer = new();
    private static readonly FhirJsonParser s_parser = new();
    private readonly ILogger<FhirSerializer> _logger;

    public FhirSerializer(ILogger<FhirSerializer> logger)
    {
        _logger = logger;
    }

    public string Serialize<T>(T resource) where T : Resource
    {
        ArgumentNullException.ThrowIfNull(resource);
        try
        {
            return s_serializer.SerializeToString(resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize {ResourceType}", typeof(T).Name);
            throw;
        }
    }

    public T? Deserialize<T>(string json) where T : Resource
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return s_parser.Parse<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize {ResourceType}", typeof(T).Name);
            return null;
        }
    }

    public Bundle? DeserializeBundle(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return s_parser.Parse<Bundle>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Bundle");
            return null;
        }
    }
}
```

---

### 4. HTTP Client Provider

#### IHttpClientProvider.cs

```csharp
namespace Gateway.API.Contracts.Http;

/// <summary>
/// Provides authenticated HTTP clients for downstream services.
/// </summary>
public interface IHttpClientProvider
{
    /// <summary>
    /// Gets an HTTP client authenticated via client credentials flow.
    /// </summary>
    /// <param name="clientName">Named HttpClient to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authenticated HttpClient or null if auth fails.</returns>
    Task<HttpClient?> GetAuthenticatedClientAsync(
        string clientName,
        CancellationToken cancellationToken = default);
}
```

#### HttpClientProvider.cs

```csharp
namespace Gateway.API.Services.Http;

using System.Net.Http.Headers;
using Gateway.API.Contracts.Http;
using Gateway.API.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Provides authenticated HTTP clients using client credentials flow.
/// </summary>
public sealed class HttpClientProvider : IHttpClientProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EpicFhirOptions _epicOptions;
    private readonly ILogger<HttpClientProvider> _logger;

    // Simple token cache (production would use IMemoryCache with expiry)
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public HttpClientProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<EpicFhirOptions> epicOptions,
        ILogger<HttpClientProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _epicOptions = epicOptions.Value;
        _logger = logger;
    }

    public async Task<HttpClient?> GetAuthenticatedClientAsync(
        string clientName,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(clientName);

        // For demo: if no token endpoint configured, return unauthenticated client
        // Epic sandbox doesn't require real OAuth for some endpoints
        if (string.IsNullOrEmpty(_epicOptions.TokenEndpoint))
        {
            _logger.LogDebug("No token endpoint configured, returning unauthenticated client");
            return client;
        }

        var token = await GetOrRefreshTokenAsync(cancellationToken);
        if (token is null)
        {
            _logger.LogError("Failed to acquire access token");
            return null;
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private async Task<string?> GetOrRefreshTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        try
        {
            using var tokenClient = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _epicOptions.ClientId,
                ["client_secret"] = _epicOptions.ClientSecret ?? ""
            });

            var response = await tokenClient.PostAsync(_epicOptions.TokenEndpoint, content, ct);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
            if (tokenResponse is null) return null;

            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // 1 min buffer

            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire token from {Endpoint}", _epicOptions.TokenEndpoint);
            return null;
        }
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")]
        string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        int ExpiresIn);
}
```

---

### 5. FHIR-Specific Errors

#### FhirErrors.cs

```csharp
namespace Gateway.API.Errors;

using Gateway.API.Abstractions;

/// <summary>
/// Predefined errors for FHIR operations.
/// </summary>
public static class FhirErrors
{
    public static readonly Error ServiceUnavailable =
        new("Fhir.ServiceUnavailable", "FHIR service is unavailable", ErrorType.Infrastructure);

    public static readonly Error Timeout =
        new("Fhir.Timeout", "FHIR request timed out", ErrorType.Infrastructure);

    public static readonly Error AuthenticationFailed =
        new("Fhir.AuthFailed", "Failed to authenticate with FHIR server", ErrorType.Unauthorized);

    public static Error NotFound(string resourceType, string id) =>
        ErrorFactory.NotFound(resourceType, id);

    public static Error InvalidResponse(string details) =>
        new("Fhir.InvalidResponse", $"Invalid FHIR response: {details}", ErrorType.Infrastructure);

    public static Error NetworkError(string message, Exception? inner = null) =>
        new("Fhir.NetworkError", message, ErrorType.Infrastructure) { Inner = inner };
}
```

---

### 6. Service Updates

#### EpicFhirContext.cs Changes

**Before:**
```csharp
var resource = await response.Content.ReadFromJsonAsync<TResource>(cancellationToken: ct);
```

**After:**
```csharp
var json = await response.Content.ReadAsStringAsync(ct);
var resource = _fhirSerializer.Deserialize<TResource>(json);
```

**Key Changes:**
- Inject `IFhirSerializer` instead of using `System.Text.Json`
- Update `ExtractResourcesFromBundle` to use `IFhirSerializer.DeserializeBundle`

#### EpicFhirClient.cs Changes

**Before:**
```csharp
public async Task<PatientInfo?> GetPatientAsync(string patientId, string accessToken, ...)
```

**After:**
```csharp
public async Task<Result<PatientInfo>> GetPatientAsync(string patientId, CancellationToken ct = default)
```

**Key Changes:**
- Return `Result<T>` instead of nullable
- Use `IHttpClientProvider` instead of direct token parameter
- Use `IFhirSerializer` for parsing FHIR resources
- Map FHIR `Patient` → `PatientInfo` after proper deserialization

#### Interface Updates

All service interfaces updated to return `Result<T>`:

```csharp
// IEpicFhirClient
Task<Result<PatientInfo>> GetPatientAsync(string patientId, CancellationToken ct = default);
Task<Result<IReadOnlyList<ConditionInfo>>> SearchConditionsAsync(string patientId, CancellationToken ct = default);
// ... etc

// IIntelligenceClient
Task<Result<PAFormData>> AnalyzeAsync(ClinicalBundle bundle, CancellationToken ct = default);

// IEpicUploader
Task<Result<string>> UploadDocumentAsync(string patientId, byte[] pdfContent, CancellationToken ct = default);

// IFhirDataAggregator
Task<Result<ClinicalBundle>> AggregateClinicalDataAsync(string patientId, CancellationToken ct = default);
```

---

### 7. Resilience Configuration

Using `Microsoft.Extensions.Http.Resilience` for standard resilience pipeline:

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddEpicFhirClient(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<EpicFhirOptions>(configuration.GetSection(EpicFhirOptions.SectionName));
    services.Configure<ResiliencyOptions>(configuration.GetSection(ResiliencyOptions.SectionName));

    services.AddHttpClient("EpicFhir", (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<EpicFhirOptions>>().Value;
        client.BaseAddress = new Uri(options.FhirBaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");
    })
    .AddStandardResilienceHandler(options =>
    {
        // Uses sensible defaults: retry, circuit breaker, timeout
        // Can customize via ResiliencyOptions if needed
    });

    return services;
}
```

---

## Migration Plan

### Phase 1: Abstractions & Configuration
1. Create `Abstractions/` directory with Result, Error, ErrorType, ErrorFactory
2. Create `Configuration/` directory with Options classes
3. Create `Errors/FhirErrors.cs`
4. Delete old `Contracts/Result.cs`

### Phase 2: FHIR Serialization
1. Create `Contracts/Fhir/IFhirSerializer.cs`
2. Create `Services/Fhir/FhirSerializer.cs`
3. Update `EpicFhirContext` to use `IFhirSerializer`
4. Register `IFhirSerializer` in DI

### Phase 3: HTTP Client Provider
1. Create `Contracts/Http/IHttpClientProvider.cs`
2. Create `Services/Http/HttpClientProvider.cs`
3. Create `Extensions/ServiceCollectionExtensions.cs`
4. Update `Program.cs` to use extension methods with resilience

### Phase 4: Service Migration
1. Update `IEpicFhirClient` interface (Result<T> returns)
2. Update `EpicFhirClient` implementation
3. Update `IIntelligenceClient` and implementation
4. Update `IEpicUploader` and implementation
5. Update `IFhirDataAggregator` and implementation
6. Update endpoint handlers to use Result.Match()

### Phase 5: Cleanup
1. Remove unused code from old Result.cs
2. Update any remaining exception-throwing code
3. Verify all services return Result<T>

---

## Testing Strategy

1. **Unit tests** for FhirSerializer with sample FHIR JSON
2. **Unit tests** for Result<T> Match/Map operations
3. **Integration tests** for HttpClientProvider token acquisition (mocked)
4. **Existing endpoint tests** updated for Result<T> responses

---

## Files to Create

| File | Description |
|------|-------------|
| `Abstractions/Result.cs` | Generic result type |
| `Abstractions/Error.cs` | Error record |
| `Abstractions/ErrorType.cs` | Error classification enum |
| `Abstractions/ErrorFactory.cs` | Common error factories |
| `Configuration/EpicFhirOptions.cs` | Epic FHIR config |
| `Configuration/IntelligenceOptions.cs` | Intelligence service config |
| `Configuration/ResiliencyOptions.cs` | Resilience settings |
| `Contracts/Fhir/IFhirSerializer.cs` | Serialization interface |
| `Contracts/Http/IHttpClientProvider.cs` | HTTP provider interface |
| `Services/Fhir/FhirSerializer.cs` | Hl7.Fhir wrapper |
| `Services/Http/HttpClientProvider.cs` | Token + client provider |
| `Errors/FhirErrors.cs` | FHIR error definitions |
| `Extensions/ServiceCollectionExtensions.cs` | DI helpers |

## Files to Modify

| File | Changes |
|------|---------|
| `Contracts/IEpicFhirClient.cs` | Return Result<T> |
| `Contracts/IIntelligenceClient.cs` | Return Result<T> |
| `Contracts/IEpicUploader.cs` | Return Result<T> |
| `Contracts/IFhirDataAggregator.cs` | Return Result<T> |
| `Services/Fhir/EpicFhirContext.cs` | Use IFhirSerializer |
| `Services/EpicFhirClient.cs` | Full rewrite with Result<T> |
| `Services/IntelligenceClient.cs` | Return Result<T> |
| `Services/EpicUploader.cs` | Return Result<T>, use provider |
| `Services/FhirDataAggregator.cs` | Return Result<T> |
| `Program.cs` | Use extension methods |
| `Endpoints/*.cs` | Handle Result<T> responses |

## Files to Delete

| File | Reason |
|------|--------|
| `Contracts/Result.cs` | Replaced by Abstractions/Result.cs + Error.cs |

---

## Success Criteria

1. All services return `Result<T>` (no null returns, no thrown exceptions for expected errors)
2. FHIR JSON parsed via `Hl7.Fhir.Serialization`
3. HTTP clients created via `IHttpClientFactory` with resilience policies
4. Single public type per file in all new/modified files
5. All existing functionality preserved
6. Demo app starts and completes PA workflow successfully
