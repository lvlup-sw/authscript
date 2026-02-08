# AuthScript

AI-powered prior authorization automation that integrates directly into Epic's clinical workflow via CDS Hooks. AuthScript automatically aggregates clinical data, reasons against payer policies, and generates completed PA forms.

## Documentation

| Quick Links | Description |
|-------------|-------------|
| [Architecture Overview](docs/ARCHITECTURE.md) | System patterns and data flow |
| [Design Document](docs/designs/2025-01-21-authscript-demo-architecture.md) | Detailed technical design |

## Quick Start

### Prerequisites

- [Node.js 20+](https://nodejs.org/) with npm
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** or **[Podman Desktop](https://podman-desktop.io/)** (required)

> **Note:** Docker Desktop (or Podman) must be running before starting the app. The Intelligence and Dashboard services run in containers.

### Setup

```bash
# Run the setup script (configures secrets automatically)
./scripts/setup.sh
```

The script configures LLM provider settings via `dotnet user-secrets`:

| Setting | Description | Default |
|---------|-------------|---------|
| `llm-provider` | LLM backend: `github`, `azure`, `gemini`, or `openai` | `github` |
| `github-token` | GitHub PAT for GitHub Models (auto-detected from `gh auth`) | — |
| `azure-openai-key` | Azure OpenAI API key | — |
| `azure-openai-endpoint` | Azure OpenAI endpoint URL | — |
| `google-api-key` | Google Gemini API key | — |
| `openai-api-key` | OpenAI API key | — |
| `openai-org-id` | OpenAI organization ID (optional) | — |

To use a different provider:

```bash
# Azure OpenAI
LLM_PROVIDER=azure AZURE_OPENAI_API_KEY=... AZURE_OPENAI_ENDPOINT=... ./scripts/setup.sh

# Google Gemini
LLM_PROVIDER=gemini GOOGLE_API_KEY=... ./scripts/setup.sh

# OpenAI
LLM_PROVIDER=openai OPENAI_API_KEY=... OPENAI_ORG_ID=... ./scripts/setup.sh
```

## IDE Setup

All IDEs use .NET Aspire to orchestrate services. The AppHost project starts all services (Gateway, Intelligence, Dashboard) with proper dependencies.

### VS Code (Recommended)

1. Install the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension
2. Open the `prior-auth` folder
3. Open the Command Palette (`Ctrl+Shift+P` / `Cmd+Shift+P`)
4. Run: **".NET: Open Solution"** → select `orchestration/AuthScript.sln`
5. In the Solution Explorer, right-click `AuthScript.AppHost` → **"Debug" → "Start New Instance"**

Alternatively, from the terminal:
```bash
cd orchestration/AuthScript.AppHost
dotnet run
```

The Aspire Dashboard opens automatically at https://localhost:15888 showing all services.

### Visual Studio / Rider

1. Open `orchestration/AuthScript.sln`
2. Set `AuthScript.AppHost` as the startup project
3. Press F5 (or Run) to start with debugging
4. The Aspire Dashboard opens automatically with all services

## Schema Synchronization

Regenerate types after modifying API contracts:

```bash
npm run sync:schemas
```

This generates:
- **TypeScript** — React Query hooks, Zod schemas (`shared/types/`, `shared/validation/`)
- **Python** — Pydantic models from Gateway spec (`apps/intelligence/src/models/generated/`)
- **C#** — Records from Intelligence spec (`apps/gateway/Gateway.API/Models/Generated/`)

CI runs schema sync and fails if generated files drift from committed versions.

### Gateway OpenAPI Extraction

Gateway uses runtime OpenAPI generation. To extract locally:

```bash
# Start Gateway, then fetch spec
curl http://localhost:5000/openapi/v1.json > apps/gateway/openapi.json
npm run sync:schemas
```

See [shared/schemas/README.md](shared/schemas/README.md) for contract ownership.

## Services

| Service | Technology | Purpose |
|---------|-----------|---------|
| **Gateway** | .NET 10, iText7, Polly | CDS Hooks, FHIR aggregation, PDF generation |
| **Intelligence** | Python 3.11, FastAPI | Clinical reasoning (GitHub/Azure/Gemini/OpenAI) |
| **Dashboard** | React 19, Vite, TanStack | Shadow dashboard + SMART fallback |

## Testing

```bash
# Run all tests
npm run test

# Run specific service tests
npm run test:dashboard
dotnet test apps/gateway/Gateway.API.Tests

# Intelligence tests (run inside container or with local Python)
cd apps/intelligence && uv run pytest
```

## Environment Variables

For local development, run `./scripts/setup.sh` (see [Setup](#setup) above).

For production or CI/CD, set these environment variables:

| Variable | Service | Description |
|----------|---------|-------------|
| `LLM_PROVIDER` | Intelligence | Provider: `github`, `azure`, `gemini`, `openai` |
| `GITHUB_TOKEN` | Intelligence | GitHub PAT for GitHub Models |
| `AZURE_OPENAI_API_KEY` | Intelligence | Azure OpenAI API key |
| `AZURE_OPENAI_ENDPOINT` | Intelligence | Azure OpenAI endpoint URL |
| `GOOGLE_API_KEY` | Intelligence | Google Gemini API key |
| `OPENAI_API_KEY` | Intelligence | OpenAI API key |
| `OPENAI_ORG_ID` | Intelligence | OpenAI organization ID (optional) |
| `Epic__ClientId` | Gateway | Epic Launchpad client ID |
| `Epic__FhirBaseUrl` | Gateway | FHIR R4 endpoint (has default) |

---

## License

Copyright 2025 AuthScript Team. All rights reserved.

This is a class project for CSE 589 at the University of Washington.
