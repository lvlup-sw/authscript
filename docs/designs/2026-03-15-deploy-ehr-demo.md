# Deploy EHR Demo to Azure

**Date:** 2026-03-15
**Status:** Draft
**Approach:** Aspire + `azd` → Azure Container Apps

## Problem

The EHR demo runs locally via .NET Aspire. We need it publicly accessible for demo
purposes (low traffic, cost-sensitive, no HA requirements).

## Approach

Use the **Aspire ↔ Azure Developer CLI (`azd`) integration** — the designed deployment
path for Aspire apps. `azd init` reads AppHost.cs and generates Bicep automatically.
All three services deploy as Azure Container Apps with managed data stores.

## Architecture

```
Internet
  │
  ├─► Dashboard (ACA) ──► nginx serving built SPA
  │     └─ /api proxy ──► Gateway (ACA)
  │
  ├─► Gateway (ACA, .NET 10)
  │     ├─► Azure DB for PostgreSQL (Flexible Server)
  │     ├─► Azure Cache for Redis
  │     └─► Intelligence (ACA, internal)
  │
  └─► Intelligence (ACA, Python/FastAPI, internal)
        └─► LLM Provider (Azure OpenAI / GitHub Models)
```

**Ingress:**
- Dashboard: external (public HTTPS endpoint)
- Gateway: external (needed for GraphQL from SPA)
- Intelligence: internal only (called by Gateway)

## Changes Required

### 1. Dashboard Dockerfile (new)

Create `apps/dashboard/Dockerfile` — multi-stage build:
- **Stage 1 (build):** Node 20, install deps, `npm run build`
- **Stage 2 (serve):** nginx:alpine, copy built assets, proxy `/api` → Gateway

The nginx config handles SPA routing (fallback to `index.html`) and reverse-proxies
`/api` requests to the Gateway service URL (injected at container start).

```dockerfile
# Build stage
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
COPY apps/dashboard/package.json apps/dashboard/
COPY shared/ shared/
RUN npm ci --workspace=apps/dashboard --workspace=shared/types --workspace=shared/validation
COPY apps/dashboard/ apps/dashboard/
ARG VITE_GATEWAY_URL=/api
ENV VITE_GATEWAY_URL=$VITE_GATEWAY_URL
RUN npm run build --workspace=shared/types && npm run build --workspace=shared/validation
RUN npm run build --workspace=apps/dashboard

# Serve stage
FROM nginx:alpine
COPY --from=build /app/apps/dashboard/dist /usr/share/nginx/html
COPY apps/dashboard/nginx.conf /etc/nginx/templates/default.conf.template
EXPOSE 80
```

### 2. Dashboard nginx.conf (new)

Create `apps/dashboard/nginx.conf` with:
- SPA fallback routing (`try_files $uri $uri/ /index.html`)
- Reverse proxy `/api` → `${GATEWAY_URL}` (resolved at container startup via envsubst)
- Cache headers for static assets

### 3. AppHost.cs Modifications

Update the AppHost to support both local dev and Azure deployment:

```csharp
// Change: Use AddDockerfile for dashboard when publishing
var dashboard = builder
    .AddDockerfile("dashboard", "../../apps/dashboard")
    .WithHttpEndpoint(port: 80, targetPort: 80, name: "dashboard-http")
    .WithExternalHttpEndpoints()
    .WaitFor(gateway)
    .WithEnvironment("GATEWAY_URL", gateway.GetEndpoint("gateway-api"));
```

**Note:** This changes the dashboard from `AddViteApp` (dev server) to
`AddDockerfile` (containerized). For local dev, we can use a launch profile
or conditional logic to preserve the Vite dev server experience. The simplest
approach is a `#if` directive or checking `builder.ExecutionContext.IsPublishMode`.

```csharp
if (builder.ExecutionContext.IsPublishMode)
{
    // Containerized for Azure deployment
    builder.AddDockerfile("dashboard", "../../apps/dashboard")
        .WithHttpEndpoint(port: 80, targetPort: 80)
        .WithExternalHttpEndpoints()
        .WaitFor(gateway)
        .WithEnvironment("GATEWAY_URL", gateway.GetEndpoint("gateway-api"));
}
else
{
    // Vite dev server for local development
    builder.AddViteApp("dashboard", "../../apps/dashboard")
        .WaitFor(gateway)
        .WithEnvironment("VITE_GATEWAY_URL", gateway.GetEndpoint("gateway-api"))
        .WithEnvironment("VITE_INTELLIGENCE_URL", intelligence.GetEndpoint("intelligence-api"));
}
```

### 4. AppHost NuGet Packages (add)

Add Azure hosting packages to `AuthScript.AppHost.csproj`:

```xml
<PackageReference Include="Aspire.Hosting.Azure.PostgreSQL" Version="13.1.0" />
<PackageReference Include="Aspire.Hosting.Azure.Redis" Version="13.1.0" />
```

These enable `azd` to provision managed Azure data stores instead of containers.

### 5. Deployment Workflow

```bash
# One-time setup
cd orchestration/AuthScript.AppHost
azd init                          # Generates azure.yaml + infra/

# Configure secrets
azd env set ATHENA_CLIENT_ID <value>
azd env set ATHENA_CLIENT_SECRET <value>
azd env set ATHENA_PRACTICE_ID 195900
azd env set GITHUB_TOKEN <value>   # or AZURE_OPENAI_API_KEY
azd env set LLM_PROVIDER github   # or azure

# Deploy
azd up                            # Provisions infra + deploys all services

# Subsequent deployments
azd deploy                        # Redeploy code only (no infra changes)

# Tear down when done
azd down                          # Deletes all Azure resources
```

### 6. Gateway CORS Configuration

The Gateway needs to allow requests from the Dashboard's ACA URL. Add the
Dashboard URL to the CORS allowed origins:

```csharp
// In Gateway Program.cs — add ACA dashboard URL to allowed origins
.WithEnvironment("AllowedOrigins", dashboard.GetEndpoint("dashboard-http"))
```

**Note:** Since the Dashboard nginx proxies `/api` to the Gateway, CORS may not
be needed at all — the browser sees same-origin requests. This depends on whether
the Dashboard makes direct calls to the Gateway URL or uses the nginx proxy.

## Cost Estimate (Demo Traffic)

| Resource | SKU | Est. Monthly Cost |
|----------|-----|-------------------|
| Azure Container Apps (3 services) | Consumption (scale-to-zero) | ~$0-5 |
| Azure DB for PostgreSQL | Burstable B1ms | ~$13 |
| Azure Cache for Redis | Basic C0 | ~$16 |
| Container Registry | Basic | ~$5 |
| **Total** | | **~$34-39/mo** |

**Cost optimization options:**
- Use container-based PostgreSQL/Redis in ACA instead of managed services (~$5/mo total)
- Use `azd down` to tear down between demo sessions ($0 when down)

## Secrets Management

Aspire parameters map to Azure Key Vault or ACA secrets automatically via `azd`.
The existing `builder.AddParameter("name", secret: true)` declarations already
mark which values are sensitive — `azd` stores these as ACA secrets.

## Out of Scope

- Custom domain / DNS (can add later via ACA custom domain binding)
- CI/CD pipeline for auto-deploy (can add GitHub Actions `azd` workflow later)
- Authentication / access control on the demo
- High availability / multi-region

## Implementation Tasks

1. Create Dashboard Dockerfile + nginx.conf
2. Update AppHost.cs with publish-mode conditional + Azure packages
3. `azd init` + configure secrets
4. `azd up` and verify
5. Test public URL end-to-end
