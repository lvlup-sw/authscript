// ===========================================================================
// AuthScript Gateway Service
// Handles CDS Hooks, FHIR data aggregation, and PDF generation
// ===========================================================================

using Gateway.API.Endpoints;
using Gateway.API.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Service Registration
// ---------------------------------------------------------------------------
builder.Services.AddOpenApi();

// Health checks
builder.Services.AddHealthChecks();

// Redis cache
builder.AddRedisClient("redis");

// PostgreSQL
builder.AddNpgsqlDataSource("authscript");

// Gateway services (FHIR, Intelligence, PDF stamping, etc.)
builder.Services.AddGatewayServices(builder.Configuration);

// CORS for dashboard
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

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
app.MapCdsHooksEndpoints();
app.MapAnalysisEndpoints();

app.Run();
