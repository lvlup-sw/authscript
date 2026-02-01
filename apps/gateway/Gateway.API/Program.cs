// ===========================================================================
// AuthScript Gateway Service
// Handles FHIR data aggregation, PA analysis, and PDF generation
// ===========================================================================

using Gateway.API;
using Gateway.API.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Infrastructure (Aspire)
// ---------------------------------------------------------------------------
builder.AddRedisClient("redis");
builder.AddNpgsqlDataSource("authscript");

// ---------------------------------------------------------------------------
// Service Registration
// ---------------------------------------------------------------------------
builder.Services
    .AddApiDocumentation()
    .AddHealthMonitoring()
    .AddCorsPolicy()
    .AddApiKeyAuthentication()
    .AddAthenaServices()      // Must be before AddFhirClients (registers AthenaOptions)
    .AddGatewayServices()
    .AddFhirClients()         // Uses IOptions<AthenaOptions> for base URL
    .AddIntelligenceClient()
    .AddNotificationServices();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware Pipeline
// ---------------------------------------------------------------------------
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "AuthScript Gateway API";
    options.Theme = ScalarTheme.DeepSpace;
    options.Authentication = new ScalarAuthenticationOptions
    {
        PreferredSecuritySchemes = ["ApiKey"],
    };
});

app.UseCors();
app.UseHealthChecks("/health");

// ---------------------------------------------------------------------------
// Endpoint Mapping
// ---------------------------------------------------------------------------
app.MapAnalysisEndpoints();
app.MapSseEndpoints();
app.MapSubmitEndpoints();
app.MapWorkItemEndpoints();

app.Run();
