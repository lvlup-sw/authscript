// ===========================================================================
// AuthScript Gateway Service
// Handles FHIR data aggregation, PA analysis, and PDF generation
// ===========================================================================

using Gateway.API;
using Gateway.API.Data;
using Gateway.API.Endpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Infrastructure (Aspire)
// ---------------------------------------------------------------------------
builder.AddRedisClient("redis");
builder.AddNpgsqlDbContext<GatewayDbContext>("authscript");

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
    .AddGatewayPersistence()  // PostgreSQL-backed stores (must be after AddGatewayServices)
    .AddFhirClients()         // Uses IOptions<AthenaOptions> for base URL
    .AddIntelligenceClient()
    .AddNotificationServices();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Database Migrations (Development)
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GatewayDbContext>();

    // Only run migrations for relational providers (not in-memory for tests)
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
}

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
app.MapPatientEndpoints();

app.Run();
