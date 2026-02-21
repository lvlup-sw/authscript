// ===========================================================================
// AuthScript Gateway Service
// Handles FHIR data aggregation, PA analysis, and PDF generation
// ===========================================================================

using Gateway.API;
using Gateway.API.Data;
using Gateway.API.Endpoints;
using Gateway.API.GraphQL.Mutations;
using Gateway.API.GraphQL.Queries;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Infrastructure (Aspire)
// ---------------------------------------------------------------------------
builder.AddRedisClient("redis");
builder.AddDatabaseMigration<GatewayDbContext>("authscript");

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

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

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
app.MapFhirEndpoints();
app.MapSseEndpoints();
app.MapSubmitEndpoints();
app.MapWorkItemEndpoints();
app.MapPatientEndpoints();

// GraphQL (mirrors REST API capabilities for dashboard)
app.MapGraphQL("/api/graphql");

app.Run();
