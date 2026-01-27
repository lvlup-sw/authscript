namespace Gateway.API.Extensions;

using Gateway.API.Configuration;
using Gateway.API.Contracts.Fhir;
using Gateway.API.Contracts.Http;
using Gateway.API.Services.Fhir;
using Gateway.API.Services.Http;
using Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extension methods for configuring Gateway services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Gateway services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<EpicFhirOptions>(configuration.GetSection(EpicFhirOptions.SectionName));
        services.Configure<IntelligenceOptions>(configuration.GetSection(IntelligenceOptions.SectionName));
        services.Configure<ResiliencyOptions>(configuration.GetSection(ResiliencyOptions.SectionName));

        // Core services
        services.AddSingleton<IFhirSerializer, FhirSerializer>();
        services.AddSingleton<IHttpClientProvider, HttpClientProvider>();

        // Epic FHIR HttpClient with resilience
        services.AddHttpClient("EpicFhir", (sp, client) =>
        {
            var options = configuration.GetSection(EpicFhirOptions.SectionName).Get<EpicFhirOptions>();
            client.BaseAddress = new Uri(options!.FhirBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");
        })
        .AddStandardResilienceHandler();

        // Intelligence HttpClient with resilience
        services.AddHttpClient("Intelligence", (sp, client) =>
        {
            var options = configuration.GetSection(IntelligenceOptions.SectionName).Get<IntelligenceOptions>();
            client.BaseAddress = new Uri(options!.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
