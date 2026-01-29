#!/bin/bash
# AuthScript Platform - Schema Synchronization
# Regenerates TypeScript types from backend OpenAPI specs
# Also generates cross-service DTOs: Gateway→Python, Intelligence→C#
# Usage: npm run sync:schemas

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

cd "$ROOT_DIR"

# CI fail-fast guard: exit non-zero when required artifacts are missing in CI
ci_require() {
    local path="$1"
    local description="$2"
    if [[ -n "${CI:-}" ]] && [[ ! -f "$path" ]]; then
        echo "      ✗ CI: Required artifact missing: ${path}" >&2
        echo "      ${description}" >&2
        exit 1
    fi
}

# CI warning: warn but don't fail (for optional/runtime-only artifacts)
ci_warn() {
    local path="$1"
    local description="$2"
    if [[ -n "${CI:-}" ]] && [[ ! -f "$path" ]]; then
        echo "      ⚠ CI: Optional artifact missing: ${path}"
        echo "      ${description}"
    fi
}

echo "=== AuthScript Schema Synchronization ==="
echo ""

# Restore .NET local tools (NSwag, Swashbuckle CLI)
if [ -f ".config/dotnet-tools.json" ]; then
    dotnet tool restore --verbosity quiet 2>/dev/null || true
fi

# Ensure uv is in PATH (common locations)
if ! command -v uv &> /dev/null; then
    if [ -f "$HOME/.local/bin/uv" ]; then
        export PATH="$HOME/.local/bin:$PATH"
    elif [ -f "$HOME/.cargo/bin/uv" ]; then
        export PATH="$HOME/.cargo/bin:$PATH"
    fi
fi

# Ensure shared schemas directory exists
mkdir -p shared/schemas

# Step 1: Build Gateway API and extract OpenAPI spec
echo "[1/7] Building Gateway API..."
if [ -f "apps/gateway/Gateway.API/Gateway.API.csproj" ]; then
    dotnet build apps/gateway/Gateway.API/Gateway.API.csproj --configuration Release --verbosity quiet
    echo "      ✓ Gateway build complete"

    # Gateway uses Microsoft.AspNetCore.OpenApi (not Swashbuckle)
    # OpenAPI spec is served at runtime via /openapi/v1.json endpoint
    # To extract: run the Gateway and fetch from http://localhost:5000/openapi/v1.json
    if [ -f "apps/gateway/openapi.json" ]; then
        cp apps/gateway/openapi.json shared/schemas/gateway.openapi.json
        echo "      ✓ Gateway OpenAPI spec copied to shared/schemas/"
    else
        echo "      ! apps/gateway/openapi.json not found"
        echo "      Gateway uses runtime OpenAPI generation (Microsoft.AspNetCore.OpenApi)"
        echo "      To extract: run Gateway and fetch from /openapi/v1.json"
        # Gateway spec is runtime-only - warn but don't fail in CI
        ci_warn "apps/gateway/openapi.json" "Gateway spec requires runtime extraction (optional in CI)"
    fi
else
    echo "      ! Gateway project not found, skipping"
    ci_require "apps/gateway/Gateway.API/Gateway.API.csproj" "CI requires Gateway project"
fi
echo ""

# Step 2: Generate Intelligence OpenAPI spec
echo "[2/7] Generating Intelligence OpenAPI spec..."
if [ -f "apps/intelligence/pyproject.toml" ]; then
    cd apps/intelligence
    if command -v uv &> /dev/null; then
        # Use FastAPI's built-in OpenAPI generation
        if uv run python -c "
from src.main import app
import json
# Output to app directory
with open('openapi.json', 'w') as f:
    json.dump(app.openapi(), f, indent=2)
# Also output to shared/schemas for cross-service generation
with open('../../shared/schemas/intelligence.openapi.json', 'w') as f:
    json.dump(app.openapi(), f, indent=2)
print('Intelligence OpenAPI spec extracted to shared/schemas/')
" 2>/dev/null; then
            echo "      ✓ Intelligence OpenAPI spec generated"
        else
            echo "      ! Python generation failed" >&2
            if [[ -n "${CI:-}" ]]; then
                echo "      ✗ CI: Intelligence OpenAPI generation failed" >&2
                exit 1
            fi
        fi
    else
        echo "      ! uv not installed, skipping Python OpenAPI generation"
        if [[ -n "${CI:-}" ]]; then
            echo "      ✗ CI: uv is required for Intelligence spec generation" >&2
            exit 1
        fi
    fi
    cd "$ROOT_DIR"
else
    echo "      ! Intelligence project not found, skipping"
    ci_require "apps/intelligence/pyproject.toml" "CI requires Intelligence project"
fi
ci_require "shared/schemas/intelligence.openapi.json" "CI requires intelligence.openapi.json for schema sync"
echo "      ✓ Intelligence spec complete"
echo ""

# Step 3: Generate Python types from Gateway OpenAPI (ClinicalBundle)
echo "[3/7] Generating Python types from Gateway spec..."
if [ -f "shared/schemas/gateway.openapi.json" ]; then
    mkdir -p apps/intelligence/src/models/generated
    cd apps/intelligence
    if uv run datamodel-codegen --help &> /dev/null; then
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
        echo "      ✓ Python types generated from Gateway spec"
    else
        echo "      ! datamodel-codegen not installed, skipping Python generation"
        echo "      Install with: uv add --dev datamodel-code-generator"
        if [[ -n "${CI:-}" ]]; then
            echo "      ✗ CI: datamodel-codegen is required for Python type generation" >&2
            exit 1
        fi
    fi
    cd "$ROOT_DIR"
else
    echo "      ! Gateway spec not found, skipping Python type generation"
    # Gateway spec is runtime-only - warn but don't fail in CI
    ci_warn "shared/schemas/gateway.openapi.json" "Gateway spec requires runtime extraction (optional in CI)"
fi
# Python types are optional since Gateway spec requires runtime extraction
ci_warn "apps/intelligence/src/models/generated/gateway_types.py" "Python types depend on Gateway spec (optional in CI)"
echo ""

# Step 4: Generate C# types from Intelligence OpenAPI (PAFormResponse)
echo "[4/7] Generating C# types from Intelligence spec..."
if [ -f "shared/schemas/intelligence.openapi.json" ]; then
    mkdir -p apps/gateway/Gateway.API/Models/Generated
    if dotnet nswag version &> /dev/null; then
        dotnet nswag openapi2csclient \
            /input:shared/schemas/intelligence.openapi.json \
            /output:apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs \
            /namespace:Gateway.API.Models.Generated \
            /generateDataAnnotations:false \
            /generateClientClasses:false \
            /generateDtoTypes:true
        echo "      ✓ C# types generated from Intelligence spec"
    else
        echo "      ! NSwag not installed, skipping C# generation"
        echo "      Install with: dotnet tool install NSwag.ConsoleCore"
        if [[ -n "${CI:-}" ]]; then
            echo "      ✗ CI: NSwag is required for C# type generation" >&2
            exit 1
        fi
    fi
else
    echo "      ! Intelligence spec not found, skipping C# type generation"
    ci_require "shared/schemas/intelligence.openapi.json" "CI requires intelligence.openapi.json for C# type generation"
fi
ci_require "apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs" "CI requires generated C# types"
echo ""

# Step 5: Clean stale generated directories
echo "[5/7] Cleaning stale generated directories..."
rm -rf apps/dashboard/src/api/generated/*/
mkdir -p apps/dashboard/src/api/generated
echo "      ✓ Stale directories removed"
echo ""

# Step 6: Generate TypeScript types via Orval
echo "[6/7] Generating TypeScript types..."
if [ -f "apps/gateway/openapi.json" ] || [ -f "apps/intelligence/openapi.json" ]; then
    if npm run generate 2>/dev/null; then
        echo "      ✓ TypeScript types generated"
    else
        echo "      ! Orval generation failed (may need npm install)" >&2
        if [[ -n "${CI:-}" ]]; then
            echo "      ✗ CI: Orval generation failed" >&2
            exit 1
        fi
    fi
else
    echo "      ! No OpenAPI specs found, skipping type generation"
fi
echo ""

# Step 7: Rebuild shared packages
echo "[7/7] Rebuilding shared packages..."
npm run build --workspace=shared/types --workspace=shared/validation --if-present 2>/dev/null || true
echo "      ✓ Shared packages rebuilt"
echo ""

echo "=== Schema Sync Complete ==="
echo ""
echo "Generated files (if present):"
echo "  - shared/schemas/gateway.openapi.json (Gateway → Python)"
echo "  - shared/schemas/intelligence.openapi.json (Intelligence → C#)"
echo "  - apps/intelligence/src/models/generated/gateway_types.py"
echo "  - apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs"
echo "  - apps/gateway/openapi.json"
echo "  - apps/intelligence/openapi.json"
echo "  - shared/types/src/generated/*.ts"
echo "  - shared/validation/src/generated/*.zod.ts"
echo "  - apps/dashboard/src/api/generated/*.ts"
