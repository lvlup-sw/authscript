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
- [Python 3.11+](https://www.python.org/) with [uv](https://docs.astral.sh/uv/)
- [Docker](https://www.docker.com/) (for Aspire containers)

### Setup

```bash
# Install Node dependencies
npm install

# Build shared packages
npm run build:shared

# Install Python dependencies
cd apps/intelligence && uv sync && cd ../..
```

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
| Dashboard | http://localhost:5173 |

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
| `npm run dev:intelligence` | Start Python service only |
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
│   └── AuthScript.AppHost/       # .NET Aspire orchestration
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
| **Intelligence** | Python 3.11, FastAPI, LangChain | Clinical reasoning, LLM orchestration |
| **Dashboard** | React 19, Vite, TanStack | Shadow dashboard + SMART fallback |

## Testing

```bash
# Run all tests
npm run test

# Run specific service tests
npm run test:dashboard
npm run test:intelligence
dotnet test apps/gateway/Gateway.API.Tests
```

## Environment Variables

Create a `.env` file or configure in your IDE:

| Variable | Service | Description |
|----------|---------|-------------|
| `Epic__ClientId` | Gateway | Epic Launchpad client ID |
| `Epic__FhirBaseUrl` | Gateway | FHIR R4 endpoint |
| `OPENAI_API_KEY` | Intelligence | GPT-4o access |
| `LLAMA_CLOUD_API_KEY` | Intelligence | LlamaParse access |

---

## License

Copyright 2025 AuthScript Team. All rights reserved.

This is a class project for CSE 589 at the University of Washington.
