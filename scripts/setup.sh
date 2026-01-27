#!/usr/bin/env bash
# ===========================================================================
# AuthScript Setup Script
# Configures dotnet user-secrets for Aspire AppHost
# ===========================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
APPHOST_PROJECT="$PROJECT_ROOT/orchestration/AuthScript.AppHost"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

info() { echo -e "${GREEN}[INFO]${NC} $1"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
error() { echo -e "${RED}[ERROR]${NC} $1"; }

# ---------------------------------------------------------------------------
# Check prerequisites
# ---------------------------------------------------------------------------
if ! command -v dotnet &> /dev/null; then
    error "dotnet CLI not found. Please install .NET SDK."
    exit 1
fi

# ---------------------------------------------------------------------------
# Initialize user-secrets if needed
# ---------------------------------------------------------------------------
info "Configuring user-secrets for AuthScript.AppHost..."

# Guard: Check if AppHost project directory exists
if [[ ! -d "$APPHOST_PROJECT" ]]; then
    error "AppHost project not found at: $APPHOST_PROJECT"
    exit 1
fi

cd "$APPHOST_PROJECT"

# ---------------------------------------------------------------------------
# LLM Provider Selection
# Options: github (default), azure, gemini, openai
# ---------------------------------------------------------------------------
LLM_PROVIDER="${LLM_PROVIDER:-github}"

dotnet user-secrets set "Parameters:llm-provider" "$LLM_PROVIDER"
info "Set llm-provider to '$LLM_PROVIDER'"

# ---------------------------------------------------------------------------
# GitHub Token (primary LLM provider)
# Priority: argument > environment > gh CLI > placeholder
# ---------------------------------------------------------------------------
GITHUB_TOKEN="${1:-${GITHUB_TOKEN:-}}"

if [[ -z "$GITHUB_TOKEN" ]] && command -v gh &> /dev/null; then
    if gh auth status &> /dev/null; then
        GITHUB_TOKEN="$(gh auth token 2>/dev/null || true)"
        if [[ -n "$GITHUB_TOKEN" ]]; then
            info "Using GitHub token from gh CLI"
        fi
    fi
fi

if [[ -n "$GITHUB_TOKEN" ]]; then
    dotnet user-secrets set "Parameters:github-token" "$GITHUB_TOKEN"
    info "Set github-token"
else
    dotnet user-secrets set "Parameters:github-token" "not-configured"
    warn "No GitHub token found - set placeholder (configure later with gh auth login)"
fi

# ---------------------------------------------------------------------------
# Azure OpenAI (optional - use placeholder if not configured)
# ---------------------------------------------------------------------------
AZURE_KEY="${AZURE_OPENAI_API_KEY:-not-configured}"
AZURE_ENDPOINT="${AZURE_OPENAI_ENDPOINT:-not-configured}"

dotnet user-secrets set "Parameters:azure-openai-key" "$AZURE_KEY"
dotnet user-secrets set "Parameters:azure-openai-endpoint" "$AZURE_ENDPOINT"

if [[ "$AZURE_KEY" != "not-configured" ]]; then
    info "Set azure-openai-key from environment"
else
    info "Set azure-openai-key placeholder (optional)"
fi

# ---------------------------------------------------------------------------
# Google Gemini (optional - use placeholder if not configured)
# ---------------------------------------------------------------------------
GOOGLE_KEY="${GOOGLE_API_KEY:-not-configured}"

dotnet user-secrets set "Parameters:google-api-key" "$GOOGLE_KEY"

if [[ "$GOOGLE_KEY" != "not-configured" ]]; then
    info "Set google-api-key from environment"
else
    info "Set google-api-key placeholder (optional)"
fi

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
echo ""
info "Setup complete! Current configuration:"
while read -r line; do
    # Mask secret values in output (but show provider)
    key=$(echo "$line" | cut -d'=' -f1)
    value=$(echo "$line" | cut -d'=' -f2- | xargs)
    if [[ "$key" == *"llm-provider"* ]]; then
        echo "  $key = $value"
    elif [[ -n "$value" && "$value" != "not-configured" ]]; then
        echo "  $key = ********"
    else
        echo "  $key = (not configured)"
    fi
done < <(dotnet user-secrets list | grep -E "llm-provider|github-token|azure-openai|google-api" || true)

echo ""
info "To switch LLM providers:"
echo ""
echo "  # GitHub Models (default - free with GitHub account)"
echo "  LLM_PROVIDER=github ./scripts/setup.sh"
echo ""
echo "  # Azure OpenAI"
echo "  LLM_PROVIDER=azure AZURE_OPENAI_API_KEY=... AZURE_OPENAI_ENDPOINT=https://... ./scripts/setup.sh"
echo ""
echo "  # Google Gemini"
echo "  LLM_PROVIDER=gemini GOOGLE_API_KEY=... ./scripts/setup.sh"
