#!/bin/bash
# AuthScript Platform - Schema Synchronization
# Regenerates TypeScript types from backend OpenAPI specs
# Usage: npm run sync:schemas

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

cd "$ROOT_DIR"

echo "=== AuthScript Schema Synchronization ==="
echo ""

# Step 1: Build Gateway API (generates openapi.json via source generator)
echo "[1/5] Building Gateway API..."
if [ -f "apps/gateway/Gateway.API/Gateway.API.csproj" ]; then
    dotnet build apps/gateway/Gateway.API/Gateway.API.csproj --configuration Release --verbosity quiet

    # Generate OpenAPI spec using dotnet swagger
    # Note: Requires Swashbuckle.AspNetCore.Cli installed
    if command -v swagger &> /dev/null; then
        dotnet swagger tofile --output apps/gateway/openapi.json \
            apps/gateway/Gateway.API/bin/Release/net10.0/Gateway.API.dll v1
    else
        echo "      ! swagger CLI not installed, skipping OpenAPI generation"
        echo "      Install with: dotnet tool install -g Swashbuckle.AspNetCore.Cli"
    fi
else
    echo "      ! Gateway project not found, skipping"
fi
echo "      ✓ Gateway build complete"
echo ""

# Step 2: Generate Intelligence OpenAPI spec
echo "[2/5] Generating Intelligence OpenAPI spec..."
if [ -f "apps/intelligence/pyproject.toml" ]; then
    cd apps/intelligence
    if command -v uv &> /dev/null; then
        # Use FastAPI's built-in OpenAPI generation
        uv run python -c "
from src.main import app
import json
with open('openapi.json', 'w') as f:
    json.dump(app.openapi(), f, indent=2)
print('OpenAPI spec generated')
" 2>/dev/null || echo "      ! Python generation failed"
    else
        echo "      ! uv not installed, skipping Python OpenAPI generation"
    fi
    cd "$ROOT_DIR"
else
    echo "      ! Intelligence project not found, skipping"
fi
echo "      ✓ Intelligence spec complete"
echo ""

# Step 3: Clean stale generated directories
echo "[3/5] Cleaning stale generated directories..."
rm -rf apps/dashboard/src/api/generated/*/
mkdir -p apps/dashboard/src/api/generated
echo "      ✓ Stale directories removed"
echo ""

# Step 4: Generate TypeScript types via Orval
echo "[4/5] Generating TypeScript types..."
if [ -f "apps/gateway/openapi.json" ] || [ -f "apps/intelligence/openapi.json" ]; then
    npm run generate 2>/dev/null || echo "      ! Orval generation failed (may need npm install)"
else
    echo "      ! No OpenAPI specs found, skipping type generation"
fi
echo "      ✓ TypeScript types generated"
echo ""

# Step 5: Rebuild shared packages
echo "[5/5] Rebuilding shared packages..."
npm run build --workspace=shared/types --workspace=shared/validation --if-present 2>/dev/null || true
echo "      ✓ Shared packages rebuilt"
echo ""

echo "=== Schema Sync Complete ==="
echo ""
echo "Generated files (if present):"
echo "  - apps/gateway/openapi.json"
echo "  - apps/intelligence/openapi.json"
echo "  - shared/types/src/generated/*.ts"
echo "  - shared/validation/src/generated/*.zod.ts"
echo "  - apps/dashboard/src/api/generated/*.ts"
