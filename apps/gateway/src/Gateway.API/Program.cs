// ===========================================================================
// AuthScript Gateway Service
// Handles CDS Hooks, FHIR data aggregation, and PDF generation
// ===========================================================================

using Gateway.API.Endpoints;
using Gateway.API.Services;

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

// HTTP clients with resilience
builder.Services.AddHttpClient<IIntelligenceClient, IntelligenceClient>(client =>
{
    var baseUrl = builder.Configuration["Intelligence:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IEpicFhirClient, EpicFhirClient>(client =>
{
    var baseUrl = builder.Configuration["Epic:FhirBaseUrl"]
        ?? "https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4";
    client.BaseAddress = new Uri(baseUrl);
});

// Application services
builder.Services.AddScoped<IFhirDataAggregator, FhirDataAggregator>();
builder.Services.AddScoped<IPdfFormStamper, PdfFormStamper>();
builder.Services.AddScoped<IEpicUploader, EpicUploader>();
builder.Services.AddSingleton<IDemoCacheService, DemoCacheService>();

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
}

app.UseCors();
app.UseHealthChecks("/health");

// ---------------------------------------------------------------------------
// Endpoint Mapping
// ---------------------------------------------------------------------------
app.MapCdsHooksEndpoints();
app.MapAnalysisEndpoints();

app.Run();
