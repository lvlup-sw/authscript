// ===========================================================================
// AuthScript Platform - Aspire AppHost
// AI-Powered Prior Authorization Demo for CSE 589
// ===========================================================================

var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Secrets (configure via dotnet user-secrets)
// ---------------------------------------------------------------------------
var epicClientId = builder.AddParameter("epic-client-id", secret: true);
var openAiApiKey = builder.AddParameter("openai-api-key", secret: true);
var llamaCloudApiKey = builder.AddParameter("llama-cloud-api-key", secret: true);

// ---------------------------------------------------------------------------
// Infrastructure
// ---------------------------------------------------------------------------
var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume("authscript-postgres-data")
    .AddDatabase("authscript");

var redis = builder
    .AddRedis("redis")
    .WithDataVolume("authscript-redis-data");

// ---------------------------------------------------------------------------
// Intelligence Service (Python/FastAPI)
// ---------------------------------------------------------------------------
var intelligence = builder.AddUvicornApp("intelligence", "../../apps/intelligence", "src.main:app")
    .WithHttpEndpoint(port: 8000, name: "http")
    .WithEnvironment("OPENAI_API_KEY", openAiApiKey)
    .WithEnvironment("LLAMA_CLOUD_API_KEY", llamaCloudApiKey)
    .WithEnvironment("DATABASE_URL", postgres)
    .WithReference(postgres)
    .WaitFor(postgres);

// ---------------------------------------------------------------------------
// Gateway Service (.NET 8 API)
// ---------------------------------------------------------------------------
var gateway = builder.AddProject<Projects.Gateway_API>("gateway")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(intelligence)
    .WithHttpEndpoint(port: 5000, name: "api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Epic__ClientId", epicClientId)
    .WithEnvironment("Epic__FhirBaseUrl", "https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4")
    .WithEnvironment("Intelligence__BaseUrl", intelligence.GetEndpoint("http"))
    .WithEnvironment("Demo__EnableCaching", "true");

// ---------------------------------------------------------------------------
// Dashboard (React/Vite)
// ---------------------------------------------------------------------------
var dashboard = builder.AddNpmApp("dashboard", "../../apps/dashboard", "dev")
    .WithHttpEndpoint(port: 5173, env: "VITE_PORT", name: "http")
    .WithEnvironment("BROWSER", "none")
    .WithEnvironment("VITE_GATEWAY_URL", gateway.GetEndpoint("api"))
    .WaitFor(gateway)
    .WithExternalHttpEndpoints();

builder.Build().Run();
