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
# Install Node dependencies
npm install

# Build shared packages
npm run build:shared
```

### Configure Secrets

The Intelligence service requires an LLM provider for clinical reasoning. Choose one of the supported providers below.

```bash
cd orchestration/AuthScript.AppHost
```

#### Option 1: GitHub Models (Recommended - Free)

```bash
dotnet user-secrets set "Parameters:github-token" "ghp_..."
```

#### Option 2: Azure OpenAI

```bash
dotnet user-secrets set "Parameters:azure-openai-key" "your-key"
dotnet user-secrets set "Parameters:azure-openai-endpoint" "https://your-resource.openai.azure.com"
```

#### Option 3: Google Gemini

```bash
dotnet user-secrets set "Parameters:google-api-key" "your-key"
```

#### PDF Parsing (Optional)

```bash
dotnet user-secrets set "Parameters:llama-cloud-api-key" "llx-..."
```

**Where to get API keys:**

| Provider | Source | Default Model | Free Tier |
|----------|--------|---------------|-----------|
| GitHub Models | [github.com/settings/tokens](https://github.com/settings/tokens) | `gpt-4.1` | Free with GitHub account |
| Azure OpenAI | [Azure Portal](https://portal.azure.com/) → Azure OpenAI | `gpt-4.1` | Pay-as-you-go |
| Google Gemini | [aistudio.google.com/apikey](https://aistudio.google.com/apikey) | `gemini-2.5-flash` | Free tier available |
| LlamaCloud | [cloud.llamaindex.ai](https://cloud.llamaindex.ai/) | — | 1,000 pages/day |

#### GitHub Education (Recommended for Students)

UW students can get **free Copilot Pro** and enhanced GitHub Models access:

1. Sign up at [GitHub Education for Students](https://github.com/education/students)
2. Verify with your `@uw.edu` or `@cs.washington.edu` email
3. Access the [Student Developer Pack](https://education.github.com/pack) benefits
4. Manage benefits at [github.com/settings/education/benefits](https://github.com/settings/education/benefits)

**What you get:**
- **Copilot Pro** — Free while enrolled (normally $10/month)
- **GitHub Models** — Higher rate limits for AI model access
- **90+ tools** — JetBrains IDEs, Azure credits, domain names, and more

> **Tip:** Use your school email for faster verification. Benefits renew automatically while you remain a verified student.

### Start Services

```bash
# From project root (npm wrapper)
npm run dev

# Or directly via Aspire (recommended)
cd orchestration/AuthScript.AppHost
dotnet run
```

This starts all services with .NET Aspire orchestration. The Aspire Dashboard opens automatically.

### Service URLs

| Service | URL |
|---------|-----|
| Aspire Dashboard | https://localhost:15888 |
| Gateway API | http://localhost:5000 |
| Intelligence API | http://localhost:8000 |
| Dashboard | http://localhost:3000 |

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

## Key Commands

| Command | Description |
|---------|-------------|
| `dotnet run --project orchestration/AuthScript.AppHost` | Start all services (Aspire) |
| `npm run dev` | Start all services (npm wrapper) |
| `npm run dev:dashboard` | Start dashboard only |
| `npm run build:containers` | Build all Docker images |
| `npm run sync:schemas` | Regenerate TypeScript from OpenAPI |
| `npm run test` | Run all tests |
| `npm run build:shared` | Build shared type packages |

## Schema Synchronization

**Before creating a PR**, ensure TypeScript types are in sync with backend schemas:

```bash
npm run sync:schemas
```

This regenerates:
- TypeScript types from OpenAPI specs
- Zod validation schemas
- React Query hooks

If you modified any API contracts in the Gateway or Intelligence services, always run schema sync and commit the generated files.

## Repository Structure

```
prior-auth/
├── apps/
│   ├── gateway/                  # .NET 10 - CDS Hooks, FHIR, PDF
│   │   ├── Gateway.API/
│   │   │   ├── Contracts/        # Interface definitions
│   │   │   ├── Endpoints/        # Minimal API endpoints
│   │   │   ├── Models/           # DTOs and domain models
│   │   │   └── Services/         # Implementation classes
│   │   └── Gateway.API.Tests/
│   ├── intelligence/             # Python - LLM reasoning
│   │   └── src/
│   └── dashboard/                # React 19 - Shadow dashboard
│       └── src/
├── orchestration/
│   └── AuthScript.AppHost/       # .NET Aspire orchestration (AppHost.cs)
├── shared/
│   ├── types/                    # @authscript/types
│   └── validation/               # @authscript/validation (Zod)
├── scripts/
│   └── build/                    # Build and sync scripts
└── docs/
    └── designs/                  # Architecture decision records
```

## Services

| Service | Technology | Purpose |
|---------|-----------|---------|
| **Gateway** | .NET 10, iText7, Polly | CDS Hooks, FHIR aggregation, PDF generation |
| **Intelligence** | Python 3.11, FastAPI | Clinical reasoning (GitHub/Azure/Gemini) |
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

For local development, use `dotnet user-secrets` (see [Configure Secrets](#configure-secrets) above).

For production or CI/CD, set these environment variables:

| Variable | Service | Description |
|----------|---------|-------------|
| `LLM_PROVIDER` | Intelligence | Provider: `github`, `azure`, `gemini` |
| `GITHUB_TOKEN` | Intelligence | GitHub PAT for GitHub Models |
| `AZURE_OPENAI_API_KEY` | Intelligence | Azure OpenAI API key |
| `AZURE_OPENAI_ENDPOINT` | Intelligence | Azure OpenAI endpoint URL |
| `GOOGLE_API_KEY` | Intelligence | Google Gemini API key |
| `LLAMA_CLOUD_API_KEY` | Intelligence | LlamaParse for PDF extraction |
| `Epic__ClientId` | Gateway | Epic Launchpad client ID |
| `Epic__FhirBaseUrl` | Gateway | FHIR R4 endpoint (has default) |

---

## License

Copyright 2025 AuthScript Team. All rights reserved.

This is a class project for CSE 589 at the University of Washington.
