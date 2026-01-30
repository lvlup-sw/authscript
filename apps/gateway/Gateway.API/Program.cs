// ===========================================================================
// AuthScript Gateway Service
// Handles FHIR data aggregation, PA analysis, and PDF generation
// ===========================================================================

using Gateway.API;
using Gateway.API.Endpoints;
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

// Gateway services
builder.Services.AddGatewayServices(builder.Configuration);
builder.Services.AddFhirClients(builder.Configuration);
builder.Services.AddIntelligenceClient(builder.Configuration);
builder.Services.AddNotificationServices();
builder.Services.AddAthenaServices(builder.Configuration);

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
app.MapAnalysisEndpoints();
app.MapSseEndpoints();
app.MapSubmitEndpoints();

app.Run();
