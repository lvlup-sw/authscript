using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Contracts.Http;
using Gateway.API.Services;
using Gateway.API.Services.Decorators;
using Gateway.API.Services.Fhir;
using Gateway.API.Services.Http;
using Gateway.API.Services.Notifications;
using Gateway.API.Services.Polling;
using Microsoft.Extensions.Caching.Hybrid;

namespace Gateway.API;

/// <summary>
/// Extension methods for configuring Gateway services.
/// </summary>
public static class DependencyExtensions
{
    /// <summary>
    /// Adds Gateway services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration options with validation
        services.AddOptions<ClinicalQueryOptions>()
            .Bind(configuration.GetSection(ClinicalQueryOptions.SectionName))
            .Validate(o => o.IsValid(), "ClinicalQueryOptions validation failed");

        services.AddOptions<Configuration.DocumentOptions>()
            .Bind(configuration.GetSection(Configuration.DocumentOptions.SectionName))
            .Validate(o => o.IsValid(), "DocumentOptions validation failed");

        services.AddOptions<CachingSettings>()
            .Bind(configuration.GetSection(CachingSettings.SectionName))
            .Validate(o => o.IsValid(), "CachingSettings validation failed");

        // HybridCache for two-tier caching (L1 in-memory + L2 Redis)
        var cachingSettings = configuration.GetSection(CachingSettings.SectionName)
            .Get<CachingSettings>() ?? new CachingSettings();
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = cachingSettings.Duration,
                LocalCacheExpiration = cachingSettings.LocalCacheDuration
            };
        });

        // Application services
        services.AddScoped<IFhirDataAggregator, FhirDataAggregator>();
        services.AddScoped<IPdfFormStamper, PdfFormStamper>();
        services.AddSingleton<IAnalysisResultStore, AnalysisResultStore>();

        return services;
    }

    /// <summary>
    /// Adds FHIR HTTP clients to the dependency injection container.
    /// Uses athenahealth FHIR base URL from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFhirClients(this IServiceCollection services, IConfiguration configuration)
    {
        var fhirBaseUrl = configuration["Athena:FhirBaseUrl"];
        if (string.IsNullOrWhiteSpace(fhirBaseUrl))
        {
            throw new InvalidOperationException("Athena:FhirBaseUrl must be configured.");
        }

        // Named HTTP client for FHIR API (used by FhirHttpClientProvider)
        services.AddHttpClient(FhirHttpClientProvider.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(fhirBaseUrl);
        });

        // Token strategy resolver (uses registered ITokenAcquisitionStrategy instances)
        services.AddSingleton<ITokenStrategyResolver, TokenStrategyResolver>();

        // Authenticated FHIR HTTP client provider
        services.AddScoped<IFhirHttpClientProvider, FhirHttpClientProvider>();

        // Low-level FHIR HTTP client (typed client with base URL)
        services.AddHttpClient<IFhirHttpClient, FhirHttpClient>(client =>
        {
            client.BaseAddress = new Uri(fhirBaseUrl);
        });

        // High-level FHIR client (uses IFhirHttpClient)
        services.AddScoped<IFhirClient, FhirClient>();

        // Document uploader (uses IFhirHttpClient)
        services.AddScoped<IDocumentUploader, DocumentUploader>();

        return services;
    }

    /// <summary>
    /// Adds the Intelligence client to the dependency injection container.
    /// Optionally wraps with caching decorator based on configuration.
    /// </summary>
    /// <remarks>
    /// STUB: Currently registers a stub implementation that returns mock data.
    /// Production will add HttpClient configuration for the Intelligence service.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIntelligenceClient(this IServiceCollection services, IConfiguration configuration)
    {
        // STUB: Register stub implementation without HTTP client
        // Production will use: services.AddHttpClient<IIntelligenceClient, IntelligenceClient>(...)
        services.AddScoped<IIntelligenceClient, IntelligenceClient>();

        // Apply caching decorator if enabled
        var cachingSettings = configuration.GetSection(CachingSettings.SectionName).Get<CachingSettings>();
        if (cachingSettings?.Enabled == true)
        {
            services.Decorate<IIntelligenceClient, CachingIntelligenceClient>();
        }

        return services;
    }

    /// <summary>
    /// Adds notification services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNotificationServices(this IServiceCollection services)
    {
        // Register NotificationHub as singleton for cross-request notifications
        services.AddSingleton<INotificationHub, NotificationHub>();

        return services;
    }

    /// <summary>
    /// Adds athenahealth-specific services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAthenaServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration options with validation
        services.AddOptions<AthenaOptions>()
            .Bind(configuration.GetSection(AthenaOptions.SectionName))
            .Validate(o => o.IsValid(), "AthenaOptions validation failed");

        // Named HttpClient for token requests
        services.AddHttpClient("Athena");

        // Token acquisition strategy and resolver
        services.AddSingleton<ITokenAcquisitionStrategy, AthenaTokenStrategy>();
        services.AddSingleton<TokenStrategyResolver>();

        // Background polling service
        services.AddSingleton<AthenaPollingService>();
        services.AddHostedService(sp => sp.GetRequiredService<AthenaPollingService>());

        // Encounter processor
        services.AddScoped<IEncounterProcessor, EncounterProcessor>();

        return services;
    }
}
