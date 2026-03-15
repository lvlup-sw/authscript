# Implementation Plan: Deploy EHR Demo to Azure

**Design:** `docs/designs/2026-03-15-deploy-ehr-demo.md`
**Date:** 2026-03-15

## Summary

Deploy the EHR demo to Azure using Aspire + `azd` → Azure Container Apps. This is
primarily infrastructure work (Dockerfiles, nginx config, Aspire modifications)
with one small code change to the GraphQL client.

## Key Insight: Proxy Architecture

The Dashboard currently uses Vite's dev proxy (`/api` → `localhost:5000`). In
production, nginx does the same. This means `graphqlClient.ts` can always use
relative `/api/graphql` — no per-environment URL construction needed.

---

## Task 1: Simplify GraphQL Client URL

**Phase:** RED → GREEN

Unify the GraphQL endpoint to always use relative `/api/graphql`. Both dev (Vite
proxy) and prod (nginx proxy) handle the `/api` reverse proxy, so the
environment-conditional URL construction is unnecessary.

### 1a. [RED] Update test expectations

- **File:** `apps/dashboard/src/api/__tests__/graphqlClient.test.ts` (new)
- Write a test that verifies the GraphQL client targets `/api/graphql`
- Expected failure: test file doesn't exist yet

### 1b. [GREEN] Simplify graphqlClient.ts

- **File:** `apps/dashboard/src/api/graphqlClient.ts`
- Change: `const GRAPHQL_ENDPOINT = '/api/graphql';`
- Remove the `import.meta.env.DEV` conditional and `getApiConfig()` import
- The `getApiConfig` function and `SecretsManager` remain (other code may use them later)

### 1c. Verify existing tests pass

- Run `npx vitest run` from `apps/dashboard/` to ensure no regressions

**Dependencies:** None
**Parallelizable:** Yes (independent of infrastructure tasks)

---

## Task 2: Create Dashboard Dockerfile

**Phase:** GREEN (infrastructure — validated by successful build)

Create a multi-stage Dockerfile that builds the React SPA and serves it via nginx.

### 2a. Create nginx.conf

- **File:** `apps/dashboard/nginx.conf` (new)
- SPA fallback: `try_files $uri $uri/ /index.html`
- Reverse proxy: `location /api/ { proxy_pass ${GATEWAY_URL}; }`
- Use `/etc/nginx/templates/default.conf.template` pattern for `envsubst`
  at container startup (nginx:alpine does this automatically)
- Static asset caching headers for `/assets/`

### 2b. Create Dockerfile

- **File:** `apps/dashboard/Dockerfile` (new)
- **Stage 1 (build):** `node:20-alpine`
  - Copy root `package.json`, `package-lock.json`, workspace configs
  - Copy `shared/` (types + validation) and `apps/dashboard/`
  - `npm ci` for relevant workspaces
  - Build shared packages, then dashboard (`npm run build`)
  - No `VITE_GATEWAY_URL` needed (relative URLs)
- **Stage 2 (serve):** `nginx:alpine`
  - Copy built assets from stage 1 to `/usr/share/nginx/html`
  - Copy `nginx.conf` to `/etc/nginx/templates/default.conf.template`
  - Expose port 80
- Build context: repository root (to access shared packages)

### 2c. Validate Docker build

- Run `docker build -f apps/dashboard/Dockerfile -t authscript-dashboard .`
- Verify container starts and serves the SPA

**Dependencies:** Task 1 (relative URLs must be in place)
**Parallelizable:** No (depends on Task 1)

---

## Task 3: Update AppHost for Azure Deployment

**Phase:** GREEN (infrastructure — validated by successful build/run)

Modify the Aspire AppHost to support both local dev (Vite) and publish mode
(containerized dashboard).

### 3a. Add Azure hosting NuGet packages

- **File:** `orchestration/AuthScript.AppHost/AuthScript.AppHost.csproj`
- Add: `Aspire.Hosting.Azure.PostgreSQL` (13.1.0)
- Add: `Aspire.Hosting.Azure.Redis` (13.1.0)

### 3b. Update AppHost.cs with publish-mode conditional

- **File:** `orchestration/AuthScript.AppHost/AppHost.cs`
- Wrap dashboard registration in `IsPublishMode` check:
  - **Publish mode:** `AddDockerfile("dashboard", ...)` with port 80,
    external endpoint, `GATEWAY_URL` env var pointing to Gateway endpoint
  - **Run mode (dev):** Keep existing `AddViteApp` configuration
- Gateway and Intelligence remain unchanged (already have Dockerfiles)

### 3c. Verify local dev still works

- Run `dotnet build` on the AppHost to verify compilation
- Optionally: `npm run dev` to verify local dev experience unchanged

**Dependencies:** Task 2 (Dashboard Dockerfile must exist for publish mode)
**Parallelizable:** No (depends on Task 2)

---

## Task 4: Add build:containers script for Dashboard

**Phase:** GREEN

### 4a. Update root package.json

- **File:** `package.json` (root)
- Add Dashboard to `build:containers` script:
  ```
  docker build -f apps/dashboard/Dockerfile -t authscript-dashboard .
  ```

**Dependencies:** Task 2
**Parallelizable:** Yes (independent of Task 3)

---

## Task 5: Azure Deployment (Interactive)

**Phase:** Deploy + verify (not automatable in plan)

This task is performed interactively with the user. It requires Azure credentials,
subscription selection, and secret configuration.

### 5a. Initialize azd

- Run `azd init` from `orchestration/AuthScript.AppHost/`
- Select Azure Container Apps as the target
- Review generated `azure.yaml` and `infra/` Bicep files

### 5b. Configure secrets

- `azd env set` for: Athena credentials, LLM provider key, practice ID

### 5c. Deploy

- `azd up` to provision infrastructure and deploy all services
- Verify all three ACA services are running
- Test Dashboard public URL → EHR demo flow end-to-end

### 5d. Document deployment

- Add deployment instructions to project README or a new `docs/DEPLOYMENT.md`
- Include: `azd up`, `azd deploy`, `azd down` workflows

**Dependencies:** Tasks 1-3
**Parallelizable:** No (requires all code changes complete)

---

## Task Dependency Graph

```
Task 1 (GraphQL client) ──► Task 2 (Dockerfile) ──► Task 3 (AppHost)
                                    │                      │
                                    ▼                      ▼
                              Task 4 (scripts)       Task 5 (deploy)
```

## Parallelization

- **Tasks 1 + 4:** Cannot parallelize (Task 4 depends on Task 2)
- **Tasks 3 + 4:** Can run in parallel after Task 2 completes
- **Task 5:** Must be last, interactive with user

## Delegation Strategy

Tasks 1-4 are small and tightly coupled — best handled as a single sequential
implementation rather than delegated to parallel agents. Task 5 is interactive
and cannot be delegated.

**Recommendation:** Implement Tasks 1-4 in a single branch, then do Task 5
interactively with the user.
