using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Services;
using Gateway.API.Services.Decorators;
using Gateway.API.Services.Fhir;
using Microsoft.Extensions.Caching.Hybrid;

namespace Gateway.API;

/// <summary>
/// Extension methods for configuring Gateway services.
/// </summary>
public static class DependencyExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Gateway services to the dependency injection container.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddGatewayServices(IConfiguration configuration)
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
            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),
                    LocalCacheExpiration = TimeSpan.FromMinutes(1)
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
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddFhirClients(IConfiguration configuration)
        {
            var fhirBaseUrl = configuration["Epic:FhirBaseUrl"]
                              ?? "https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4";

            // Low-level FHIR HTTP client
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
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddIntelligenceClient(IConfiguration configuration)
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
    }
}
