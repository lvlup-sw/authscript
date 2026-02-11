// =============================================================================
// <copyright file="DependencyExtensions.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using System.Text.Json.Serialization;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Contracts.Http;
using Gateway.API.Filters;
using Gateway.API.Services;
using Gateway.API.Services.Decorators;
using Gateway.API.Services.Fhir;
using Gateway.API.Services.Http;
using Gateway.API.Services.Notifications;
using Gateway.API.Services.Polling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Gateway.API.Data;

namespace Gateway.API;

/// <summary>
/// Extension methods for configuring Gateway services.
/// </summary>
public static class DependencyExtensions
{
    private const string CorsOriginsConfigKey = "Cors:Origins";
    private const string DefaultCorsOrigin = "http://localhost:5173";

    /// <summary>
    /// Adds CORS policy for the dashboard.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to read allowed origins from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Read origins from config or use default for development
        var origins = configuration?.GetSection(CorsOriginsConfigKey).Get<string[]>()
            ?? [DefaultCorsOrigin];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        return services;
    }

    /// <summary>
    /// Adds API key authentication services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddOptions<ApiKeySettings>()
            .BindConfiguration(ApiKeySettings.SectionName);

        services.AddSingleton<IApiKeyValidator, ApiKeyValidator>();

        return services;
    }

    /// <summary>
    /// Adds OpenAPI documentation and configures JSON serialization.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
        });

        // Configure JSON to use string names for enums (accepts both string and int on input)
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }

    /// <summary>
    /// Adds health check monitoring.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealthMonitoring(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    /// <summary>
    /// Adds Gateway services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGatewayServices(this IServiceCollection services)
    {
        // Configuration options with validation
        services.AddOptions<ClinicalQueryOptions>()
            .BindConfiguration(ClinicalQueryOptions.SectionName)
            .Validate(o => o.IsValid(), "ClinicalQueryOptions validation failed");

        services.AddOptions<Configuration.DocumentOptions>()
            .BindConfiguration(Configuration.DocumentOptions.SectionName)
            .Validate(o => o.IsValid(), "DocumentOptions validation failed");

        services.AddOptions<CachingSettings>()
            .BindConfiguration(CachingSettings.SectionName)
            .Validate(o => o.IsValid(), "CachingSettings validation failed");

        // HybridCache for two-tier caching (L1 in-memory + L2 Redis)
        // Configured via IConfigureOptions<HybridCacheOptions>
        services.AddHybridCache();
        services.AddSingleton<IConfigureOptions<HybridCacheOptions>, ConfigureHybridCacheOptions>();

        // Application services
        services.AddScoped<IFhirDataAggregator, FhirDataAggregator>();
        services.AddScoped<IPdfFormStamper, PdfFormStamper>();
        services.AddSingleton<IAnalysisResultStore, AnalysisResultStore>();
        services.AddSingleton<MockDataService>();

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL persistence services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// EF Core DbContext is registered by Aspire via builder.AddNpgsqlDbContext.
    /// This method registers the stores that use the DbContext.
    /// </remarks>
    public static IServiceCollection AddGatewayPersistence(this IServiceCollection services)
    {
        services.AddScoped<IWorkItemStore, PostgresWorkItemStore>();
        services.AddScoped<IPatientRegistry, PostgresPatientRegistry>();

        return services;
    }

    /// <summary>
    /// Adds FHIR HTTP clients to the dependency injection container.
    /// Uses athenahealth FHIR base URL from AthenaOptions.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFhirClients(this IServiceCollection services)
    {
        // Named HTTP client for FHIR API (used by FhirHttpClientProvider)
        // Base URL configured via IOptions<AthenaOptions>
        services.AddHttpClient(FhirHttpClientProvider.HttpClientName)
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<AthenaOptions>>().Value;
                client.BaseAddress = new Uri(options.FhirBaseUrl);
            });

        // Token strategy resolver (uses registered ITokenAcquisitionStrategy instances)
        // Register both interface and concrete type for DI consumers that use either
        services.AddSingleton<TokenStrategyResolver>();
        services.AddSingleton<ITokenStrategyResolver>(sp => sp.GetRequiredService<TokenStrategyResolver>());

        // Authenticated FHIR HTTP client provider
        services.AddScoped<IFhirHttpClientProvider, FhirHttpClientProvider>();

        // Low-level FHIR HTTP client (typed client with base URL)
        // Base URL configured via IOptions<AthenaOptions>
        services.AddHttpClient<IFhirHttpClient, FhirHttpClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<AthenaOptions>>().Value;
                client.BaseAddress = new Uri(options.FhirBaseUrl);
            });

        // FHIR token provider (manages OAuth tokens for FHIR API calls)
        // Singleton because FhirTokenProvider is stateless and its dependencies
        // (ITokenStrategyResolver, ILogger) are also singletons
        services.AddSingleton<IFhirTokenProvider, FhirTokenProvider>();

        // High-level FHIR client (uses IFhirHttpClient)
        services.AddScoped<IFhirClient, FhirClient>();

        // Document uploader (uses IFhirHttpClient)
        services.AddScoped<IDocumentUploader, DocumentUploader>();

        return services;
    }

    /// <summary>
    /// Adds the Intelligence client to the dependency injection container.
    /// Wraps with caching decorator (decorator checks Enabled setting at runtime).
    /// </summary>
    /// <remarks>
    /// STUB: Currently registers a stub implementation that returns mock data.
    /// Production will add HttpClient configuration for the Intelligence service.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIntelligenceClient(this IServiceCollection services)
    {
        // STUB: Register stub implementation without HTTP client
        // Production will use: services.AddHttpClient<IIntelligenceClient, IntelligenceClient>(...)
        services.AddScoped<IIntelligenceClient, IntelligenceClient>();

        // Apply caching decorator (checks Enabled setting at runtime)
        services.Decorate<IIntelligenceClient, CachingIntelligenceClient>();

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
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAthenaServices(this IServiceCollection services)
    {
        // Configuration options with validation
        services.AddOptions<AthenaOptions>()
            .BindConfiguration(AthenaOptions.SectionName)
            .Validate(o => o.IsValid(), "AthenaOptions validation failed");

        // Named HttpClient for token requests
        services.AddHttpClient("Athena");

        // Token acquisition strategy (resolver registered in AddFhirClients)
        services.AddSingleton<ITokenAcquisitionStrategy, AthenaTokenStrategy>();

        // Background polling service
        services.AddSingleton<AthenaPollingService>();
        services.AddHostedService(sp => sp.GetRequiredService<AthenaPollingService>());

        // Encounter processor
        services.AddScoped<IEncounterProcessor, EncounterProcessor>();

        return services;
    }

    /// <summary>
    /// Adds database migration services for a specific DbContext.
    /// Runs migrations automatically on startup via a background service.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type to configure.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionName">The Aspire connection name to use.</param>
    /// <param name="setupAction">Optional configuration for migration options.</param>
    /// <returns>The modified host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddDatabaseMigration<TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<MigrationServiceOptions>? setupAction = null)
        where TContext : DbContext
    {
        // Add PostgreSQL context using Aspire pattern
        builder.AddNpgsqlDbContext<TContext>(connectionName,
            configureDbContextOptions: options =>
            {
                options.UseNpgsql();

                // Enable detailed logging in Development only
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

        // Configure options
        builder.Services.AddOptions<MigrationServiceOptions>();
        if (setupAction is not null)
        {
            builder.Services.Configure(setupAction);
        }

        // Add migration service as hosted service
        builder.Services.AddHostedService<MigrationService<TContext>>();

        // Add migration health check
        builder.Services.AddHealthChecks()
            .AddCheck<MigrationHealthCheck>("migrations", tags: ["ready"]);

        // Add OpenTelemetry tracing for migrations
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(MigrationService<TContext>.ActivitySourceName));

        return builder;
    }
}
