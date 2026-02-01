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
    .AddGatewayServices(builder.Configuration)
    .AddFhirClients(builder.Configuration)
    .AddIntelligenceClient(builder.Configuration)
    .AddNotificationServices()
    .AddAthenaServices(builder.Configuration);

var app = builder.Build();

// ---------------------------------------------------------------------------
// Middleware Pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "AuthScript Gateway API";
        options.Theme = ScalarTheme.DeepSpace;
    });
}

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
