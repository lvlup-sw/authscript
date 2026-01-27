# Schema Synchronization: .NET Gateway ↔ Python Intelligence

**Date**: 2026-01-26
**Status**: Draft
**Author**: Claude (ideation session)

## Problem Statement

The AuthScript platform has two backend services that communicate via HTTP:
- **Gateway** (.NET 10): Aggregates FHIR data, sends `ClinicalBundle` to Intelligence
- **Intelligence** (Python FastAPI): Analyzes clinical data, returns `PAFormResponse`

These services define equivalent data models in their respective languages, creating a maintenance burden and risk of schema drift. When one side changes a field name, type, or structure, the other side may not be updated, causing runtime failures.

## Goals

1. **Prevent drift**: Catch schema mismatches before runtime
2. **Single source of truth**: Each producer owns its contract definition
3. **Auto-regenerate**: Changes to source models trigger consumer type regeneration
4. **CI integration**: Fail builds when generated code diverges from committed code

## Non-Goals

- Generating REST clients (typed fetch wrappers) — focus on DTOs only
- Dashboard/TypeScript synchronization (may be deprecated)
- Migrating to gRPC or other RPC protocols

## Architecture

### Contract Ownership (Producer-Owns-Contract)

| Contract | Producer | Consumer | Direction |
|----------|----------|----------|-----------|
| `ClinicalBundle` | Gateway (.NET) | Intelligence (Python) | Gateway → Intelligence |
| `PAFormResponse` | Intelligence (Python) | Gateway (.NET) | Intelligence → Gateway |

Each service generates an OpenAPI spec describing the types it **produces**. Consumer types are generated from the producer's spec.

### Data Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Schema Sync Pipeline                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────┐         ┌──────────────┐         ┌──────────────┐    │
│  │   Gateway    │         │   Shared     │         │ Intelligence │    │
│  │   (.NET)     │         │   Schemas    │         │   (Python)   │    │
│  └──────┬───────┘         └──────────────┘         └──────┬───────┘    │
│         │                        ▲                        │            │
│         │                        │                        │            │
│         ▼                        │                        ▼            │
│  ┌──────────────┐                │                 ┌──────────────┐    │
│  │ openapi.json │                │                 │ openapi.json │    │
│  │ (Gateway)    │────────────────┼─────────────────│(Intelligence)│    │
│  └──────┬───────┘                │                 └──────┬───────┘    │
│         │                        │                        │            │
│         │    datamodel-codegen   │   NSwag/Kiota         │            │
│         │         ▼              │        ▼               │            │
│         │  ┌─────────────┐       │  ┌─────────────┐       │            │
│         └─►│ Python DTOs │       │  │  C# DTOs    │◄──────┘            │
│            │ (generated) │       │  │ (generated) │                    │
│            └─────────────┘       │  └─────────────┘                    │
│                 │                │        │                            │
│                 ▼                │        ▼                            │
│            apps/intelligence/    │   apps/gateway/                     │
│            src/models/generated/ │   Gateway.API/Models/Generated/     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Detailed Design

### 1. OpenAPI Spec Extraction

#### Gateway (Producer of ClinicalBundle)

The Gateway needs to export an OpenAPI spec describing the `ClinicalBundle` and related types it sends to Intelligence. Since the Gateway is the **caller** (not exposing an endpoint for this), we'll create a dedicated schema endpoint or use build-time extraction.

**Option A: Build-time extraction with dotnet-openapi**

```bash
# In sync-schemas.sh
dotnet build apps/gateway/Gateway.API
dotnet swagger tofile \
  --output shared/schemas/gateway.openapi.json \
  apps/gateway/Gateway.API/bin/Release/net10.0/Gateway.API.dll v1
```

**Option B: Schema-only endpoint**

Add a minimal endpoint that returns the schema without exposing it in production:

```csharp
// Endpoints/SchemaEndpoints.cs
public static void MapSchemaEndpoints(this WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/schemas/clinical-bundle", () =>
            TypedResults.Ok(JsonSchema.FromType<ClinicalBundle>()));
    }
}
```

**Recommendation**: Option A (build-time extraction) is cleaner and doesn't require runtime changes.

#### Intelligence (Producer of PAFormResponse)

FastAPI already generates OpenAPI automatically. Extract it at build time:

```bash
# In sync-schemas.sh
cd apps/intelligence
uv run python -c "
from src.main import app
import json
with open('../../shared/schemas/intelligence.openapi.json', 'w') as f:
    json.dump(app.openapi(), f, indent=2)
"
```

### 2. Consumer Type Generation

#### Python Types from Gateway OpenAPI (ClinicalBundle)

Use `datamodel-code-generator` to generate Pydantic models:

```bash
# Install
uv add --dev datamodel-code-generator

# Generate
datamodel-codegen \
  --input shared/schemas/gateway.openapi.json \
  --output apps/intelligence/src/models/generated/gateway_types.py \
  --input-file-type openapi \
  --output-model-type pydantic_v2.BaseModel \
  --target-python-version 3.11 \
  --use-standard-collections \
  --use-union-operator \
  --field-constraints \
  --strict-nullable
```

**Generated output** (example):

```python
# apps/intelligence/src/models/generated/gateway_types.py
# Auto-generated from Gateway OpenAPI spec - DO NOT EDIT

from __future__ import annotations
from pydantic import BaseModel, Field
from datetime import date

class PatientInfo(BaseModel):
    id: str
    given_name: str | None = None
    family_name: str | None = None
    birth_date: date | None = None
    gender: str | None = None
    member_id: str | None = None

class ClinicalBundle(BaseModel):
    patient_id: str
    patient: PatientInfo | None = None
    conditions: list[ConditionInfo] = Field(default_factory=list)
    observations: list[ObservationInfo] = Field(default_factory=list)
    procedures: list[ProcedureInfo] = Field(default_factory=list)
    documents: list[DocumentInfo] = Field(default_factory=list)
```

#### C# Types from Intelligence OpenAPI (PAFormResponse)

Use `NSwag` or `Kiota` to generate C# records:

**Option A: NSwag**

```bash
# Install
dotnet tool install -g NSwag.ConsoleCore

# Generate
nswag openapi2cscontroller \
  /input:shared/schemas/intelligence.openapi.json \
  /output:apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs \
  /namespace:Gateway.API.Models.Generated \
  /generateDataAnnotations:false \
  /generateRecordTypes:true
```

**Option B: Kiota (Microsoft's newer tool)**

```bash
# Install
dotnet tool install -g Microsoft.OpenApi.Kiota

# Generate models only (no client)
kiota generate \
  --openapi shared/schemas/intelligence.openapi.json \
  --output apps/gateway/Gateway.API/Models/Generated \
  --language CSharp \
  --class-name IntelligenceModels \
  --namespace-name Gateway.API.Models.Generated
```

**Recommendation**: NSwag with `--generateRecordTypes:true` for idiomatic C# records.

**Generated output** (example):

```csharp
// apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs
// Auto-generated from Intelligence OpenAPI spec - DO NOT EDIT

namespace Gateway.API.Models.Generated;

public sealed record PAFormResponse
{
    [JsonPropertyName("patient_name")]
    public required string PatientName { get; init; }

    [JsonPropertyName("patient_dob")]
    public required string PatientDob { get; init; }

    [JsonPropertyName("confidence_score")]
    public required double ConfidenceScore { get; init; }

    // ... etc
}

public sealed record EvidenceItem
{
    [JsonPropertyName("criterion_id")]
    public required string CriterionId { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }  // "MET" | "NOT_MET" | "UNCLEAR"

    // ... etc
}
```

### 3. Updated sync-schemas.sh Script

```bash
#!/bin/bash
# scripts/build/sync-schemas.sh
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

cd "$ROOT_DIR"

echo "=== AuthScript Schema Synchronization ==="
echo ""

# Create shared schemas directory
mkdir -p shared/schemas

# Step 1: Extract Gateway OpenAPI spec
echo "[1/6] Extracting Gateway OpenAPI spec..."
if [ -f "apps/gateway/Gateway.API/Gateway.API.csproj" ]; then
    dotnet build apps/gateway/Gateway.API/Gateway.API.csproj \
        --configuration Release --verbosity quiet

    if command -v swagger &> /dev/null; then
        swagger tofile --output shared/schemas/gateway.openapi.json \
            apps/gateway/Gateway.API/bin/Release/net10.0/Gateway.API.dll v1
        echo "      ✓ Gateway spec extracted"
    else
        echo "      ! swagger CLI not installed"
        echo "      Install: dotnet tool install -g Swashbuckle.AspNetCore.Cli"
    fi
fi

# Step 2: Extract Intelligence OpenAPI spec
echo "[2/6] Extracting Intelligence OpenAPI spec..."
if [ -f "apps/intelligence/pyproject.toml" ]; then
    cd apps/intelligence
    uv run python -c "
from src.main import app
import json
with open('../../shared/schemas/intelligence.openapi.json', 'w') as f:
    json.dump(app.openapi(), f, indent=2)
print('Intelligence spec extracted')
"
    cd "$ROOT_DIR"
    echo "      ✓ Intelligence spec extracted"
fi

# Step 3: Generate Python types from Gateway spec
echo "[3/6] Generating Python types from Gateway spec..."
if [ -f "shared/schemas/gateway.openapi.json" ]; then
    mkdir -p apps/intelligence/src/models/generated
    cd apps/intelligence
    uv run datamodel-codegen \
        --input ../../shared/schemas/gateway.openapi.json \
        --output src/models/generated/gateway_types.py \
        --input-file-type openapi \
        --output-model-type pydantic_v2.BaseModel \
        --target-python-version 3.11 \
        --use-standard-collections \
        --use-union-operator \
        --field-constraints \
        --strict-nullable
    cd "$ROOT_DIR"
    echo "      ✓ Python types generated"
else
    echo "      ! Gateway spec not found, skipping"
fi

# Step 4: Generate C# types from Intelligence spec
echo "[4/6] Generating C# types from Intelligence spec..."
if [ -f "shared/schemas/intelligence.openapi.json" ]; then
    mkdir -p apps/gateway/Gateway.API/Models/Generated

    if command -v nswag &> /dev/null; then
        nswag openapi2cscontroller \
            /input:shared/schemas/intelligence.openapi.json \
            /output:apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs \
            /namespace:Gateway.API.Models.Generated \
            /generateDataAnnotations:false \
            /generateRecordTypes:true
        echo "      ✓ C# types generated"
    else
        echo "      ! NSwag not installed"
        echo "      Install: dotnet tool install -g NSwag.ConsoleCore"
    fi
else
    echo "      ! Intelligence spec not found, skipping"
fi

# Step 5: Verify generated files
echo "[5/6] Verifying generated files..."
ERRORS=0

if [ -f "apps/intelligence/src/models/generated/gateway_types.py" ]; then
    cd apps/intelligence
    uv run python -c "from src.models.generated.gateway_types import *" 2>/dev/null \
        && echo "      ✓ Python types valid" \
        || { echo "      ✗ Python types invalid"; ERRORS=$((ERRORS+1)); }
    cd "$ROOT_DIR"
fi

if [ -f "apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs" ]; then
    dotnet build apps/gateway/Gateway.API/Gateway.API.csproj --verbosity quiet 2>/dev/null \
        && echo "      ✓ C# types valid" \
        || { echo "      ✗ C# types invalid"; ERRORS=$((ERRORS+1)); }
fi

# Step 6: Summary
echo "[6/6] Summary"
echo ""
echo "Generated files:"
echo "  - shared/schemas/gateway.openapi.json"
echo "  - shared/schemas/intelligence.openapi.json"
echo "  - apps/intelligence/src/models/generated/gateway_types.py"
echo "  - apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs"
echo ""

if [ $ERRORS -gt 0 ]; then
    echo "=== Schema Sync FAILED ($ERRORS errors) ==="
    exit 1
else
    echo "=== Schema Sync Complete ==="
fi
```

### 4. CI Integration

Your existing CI already runs `sync-schemas`. Add a drift detection step:

```yaml
# .github/workflows/ci.yml
schema-sync:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Setup Python
      uses: actions/setup-python@v5
      with:
        python-version: '3.11'

    - name: Install uv
      run: curl -LsSf https://astral.sh/uv/install.sh | sh

    - name: Install tools
      run: |
        dotnet tool install -g Swashbuckle.AspNetCore.Cli
        dotnet tool install -g NSwag.ConsoleCore

    - name: Run schema sync
      run: npm run sync:schemas

    - name: Check for drift
      run: |
        if [[ -n $(git status --porcelain shared/schemas apps/*/Models/Generated apps/*/src/models/generated) ]]; then
          echo "::error::Generated schemas have drifted from source!"
          git diff --stat
          exit 1
        fi
```

### 5. Pre-commit Hook (Optional)

For local development with rapid iteration, add a pre-commit hook:

```bash
# .husky/pre-commit (if using Husky)
#!/bin/sh
npm run sync:schemas
git add shared/schemas apps/*/Models/Generated apps/*/src/models/generated
```

Or with `pre-commit` framework:

```yaml
# .pre-commit-config.yaml
repos:
  - repo: local
    hooks:
      - id: schema-sync
        name: Sync schemas
        entry: npm run sync:schemas
        language: system
        files: '\.(cs|py)$'
        pass_filenames: false
```

### 6. File Structure After Implementation

```
prior-auth/
├── shared/
│   ├── schemas/                          # NEW: Extracted OpenAPI specs
│   │   ├── gateway.openapi.json
│   │   └── intelligence.openapi.json
│   ├── types/                            # Existing (TypeScript)
│   └── validation/                       # Existing (Zod)
├── apps/
│   ├── gateway/
│   │   └── Gateway.API/
│   │       └── Models/
│   │           ├── Generated/            # NEW: Generated from Intelligence
│   │           │   └── IntelligenceTypes.cs
│   │           ├── PAFormData.cs         # Existing (may become obsolete)
│   │           └── ClinicalBundle.cs     # Existing (source of truth)
│   └── intelligence/
│       └── src/
│           └── models/
│               ├── generated/            # NEW: Generated from Gateway
│               │   └── gateway_types.py
│               ├── pa_form.py            # Existing (source of truth)
│               └── clinical_bundle.py    # Existing (may become obsolete)
└── scripts/
    └── build/
        └── sync-schemas.sh               # UPDATED
```

## Migration Strategy

### Phase 1: Infrastructure (This PR)
1. Update `sync-schemas.sh` with dual extraction and generation
2. Add `shared/schemas/` directory
3. Install code generation tools
4. Generate initial types

### Phase 2: Gradual Adoption
1. Import generated types alongside existing types
2. Add type aliases for compatibility:
   ```python
   # Compatibility alias
   from .generated.gateway_types import ClinicalBundle as ClinicalBundleGenerated
   ClinicalBundle = ClinicalBundleGenerated  # Use generated type
   ```
3. Update consumers to use generated types
4. Deprecate hand-written duplicates

### Phase 3: Cleanup
1. Remove manual duplicate type definitions
2. Update all imports to use generated types
3. Remove compatibility aliases

## Tooling Requirements

| Tool | Installation | Purpose |
|------|--------------|---------|
| `swagger` CLI | `dotnet tool install -g Swashbuckle.AspNetCore.Cli` | Extract Gateway OpenAPI |
| `nswag` | `dotnet tool install -g NSwag.ConsoleCore` | Generate C# from OpenAPI |
| `datamodel-code-generator` | `uv add --dev datamodel-code-generator` | Generate Python from OpenAPI |

## Alternatives Considered

### JSON Schema (Approach B)
- **Pros**: Simpler, more portable
- **Cons**: Loses endpoint info, less mature C# extraction
- **Decision**: Rejected — OpenAPI is already generated by both frameworks

### Protobuf (Approach C)
- **Pros**: Strictest contracts, fast generation
- **Cons**: Requires learning proto syntax, invasive migration
- **Decision**: Rejected — Higher migration cost, REST semantics work well

## Open Questions

1. **NSwag vs Kiota**: Both generate C# from OpenAPI. NSwag is mature; Kiota is Microsoft's newer tool. Recommend NSwag for stability.

2. **Watch mode**: Should we add `chokidar` or similar for auto-regeneration during development?

3. **Versioning**: Should OpenAPI specs be versioned (v1, v2) or use a single rolling spec?

## Success Metrics

- [ ] `npm run sync:schemas` generates types for both directions
- [ ] CI fails when generated code differs from committed code
- [ ] Changing a field in Gateway triggers Python type regeneration
- [ ] Changing a field in Intelligence triggers C# type regeneration
- [ ] Build succeeds with generated types
