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
npm run dev
```

This starts all services with .NET Aspire orchestration.

### Service URLs

| Service | URL |
|---------|-----|
| Aspire Dashboard | https://localhost:15888 |
| Gateway API | http://localhost:5100 |
| Intelligence API | http://localhost:8000 |
| Dashboard | http://localhost:5173 |

## IDE Setup

### VS Code (Recommended)

1. Open the `prior-auth` folder
2. Install recommended extensions when prompted
3. Use the integrated terminal to run commands
4. **Run services:** `npm run dev` or use the Tasks: Run Task command

### Visual Studio

1. Open `orchestration/AuthScript.sln`
2. Set `AuthScript.AppHost` as the startup project
3. Press F5 to start with debugging
4. For frontend: open a terminal and run `npm run dev:dashboard`

### JetBrains Rider

1. Open the `prior-auth` folder as a directory
2. Open `orchestration/AuthScript.sln` for backend work
3. Use the built-in terminal for npm commands
4. Configure a compound run configuration for full-stack development

## Key Commands

| Command | Description |
|---------|-------------|
| `npm run dev` | Start all services (Aspire) |
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
