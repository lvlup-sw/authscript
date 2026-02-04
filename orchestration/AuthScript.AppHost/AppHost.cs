// ===========================================================================
// AuthScript Platform - Aspire AppHost
// AI-Powered Prior Authorization Demo for CSE 589
// ===========================================================================

var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Secrets (configure via dotnet user-secrets)
// LLM Provider: Set LLM_PROVIDER to "github", "azure", "gemini", or "openai"
// ---------------------------------------------------------------------------
var epicClientId = builder.AddParameter("epic-client-id", secret: true);

// GitHub Models (default - free with GitHub account)
var githubToken = builder.AddParameter("github-token", secret: true);

// Azure OpenAI (alternative)
var azureOpenAiKey = builder.AddParameter("azure-openai-key", secret: true);
var azureOpenAiEndpoint = builder.AddParameter("azure-openai-endpoint");

// Google Gemini (alternative)
var googleApiKey = builder.AddParameter("google-api-key", secret: true);

// LLM Provider selection (github, azure, gemini, or openai)
var llmProvider = builder.AddParameter("llm-provider");

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
// Intelligence Service (Python/FastAPI in container)
// Containerized to avoid host Python/uv environment issues
// ---------------------------------------------------------------------------
var intelligence = builder
    .AddDockerfile("intelligence", "../../apps/intelligence")
    .WithHttpEndpoint(port: 8000, targetPort: 8000, name: "intelligence-api")
    // LLM Provider config (configurable via user-secrets)
    .WithEnvironment("LLM_PROVIDER", llmProvider)
    .WithEnvironment("GITHUB_TOKEN", githubToken)
    .WithEnvironment("AZURE_OPENAI_API_KEY", azureOpenAiKey)
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", azureOpenAiEndpoint)
    .WithEnvironment("GOOGLE_API_KEY", googleApiKey)
    .WithEnvironment("DATABASE_URL", postgres.Resource.ConnectionStringExpression)
    .WaitFor(postgres);

// ---------------------------------------------------------------------------
// Gateway Service (.NET 10 API)
// Uses AddProject for native Aspire integration (debugging, hot reload)
// ---------------------------------------------------------------------------
var gateway = builder
    .AddProject<Projects.Gateway_API>("gateway")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WaitFor(intelligence)
    .WithHttpEndpoint(port: 5000, name: "gateway-api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Epic__ClientId", epicClientId)
    .WithEnvironment("Epic__FhirBaseUrl", "https://fhir.epic.com/interconnect-fhir-oauth/api/FHIR/R4")
    .WithEnvironment("Intelligence__BaseUrl", intelligence.GetEndpoint("intelligence-api"))
    .WithEnvironment("Demo__EnableCaching", "true");

// ---------------------------------------------------------------------------
// Dashboard: Run separately with `npm run dev:dashboard` from repo root
// ---------------------------------------------------------------------------

builder.Build().Run();
