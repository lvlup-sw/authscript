#!/usr/bin/env bash
# ===========================================================================
# AuthScript Platform — Developer Secrets Setup
# Run once on a new dev machine to configure all required user-secrets.
# ===========================================================================
#
# Usage:
#   ./scripts/setup-secrets.sh          # Uses hardcoded defaults (zero input)
#
# All sandbox credentials are hardcoded. To override any value, set the
# corresponding environment variable before running:
#
#   LLM_PROVIDER=azure AZURE_OPENAI_API_KEY=sk-... ./scripts/setup-secrets.sh
# ===========================================================================

set -euo pipefail

APPHOST_SECRETS_ID="authscript-apphost"
GATEWAY_SECRETS_ID="e2b3d9aa-6adc-41e7-8076-8466d9c21dbf"

# ---------------------------------------------------------------------------
# Shared sandbox defaults (athenahealth preview sandbox — not personal secrets)
# ---------------------------------------------------------------------------
ATHENA_CLIENT_ID="0oa10sj7fqpmhiLoV298"
ATHENA_CLIENT_SECRET="B5gaw0w28FC1zR2DTFtLVgULOg8G1dDxibWJs7bxqEJzDRup72-kHpXkpWtwhys-"
ATHENA_PRACTICE_ID="195900"
DEFAULT_LLM_PROVIDER="github"
GITHUB_TOKEN_DEFAULT="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"

# Colors (disabled if not a terminal)
if [ -t 1 ]; then
  BOLD='\033[1m' DIM='\033[2m' GREEN='\033[0;32m' YELLOW='\033[0;33m'
  CYAN='\033[0;36m' RED='\033[0;31m' RESET='\033[0m'
else
  BOLD='' DIM='' GREEN='' YELLOW='' CYAN='' RED='' RESET=''
fi

info()  { echo -e "${CYAN}ℹ${RESET}  $*"; }
ok()    { echo -e "${GREEN}✓${RESET}  $*"; }
warn()  { echo -e "${YELLOW}⚠${RESET}  $*"; }
error() { echo -e "${RED}✗${RESET}  $*" >&2; }

set_secret() {
  local key="$1" value="$2"
  if [[ -n "$value" ]]; then
    dotnet user-secrets set "Parameters:${key}" "$value" --id "$APPHOST_SECRETS_ID" > /dev/null
  fi
}

# ---------------------------------------------------------------------------
# Pre-flight
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [[ ! -f "$REPO_ROOT/orchestration/AuthScript.AppHost/AppHost.cs" ]]; then
  error "Cannot find AppHost. Run this script from the repo root."
  exit 1
fi

if ! command -v dotnet &> /dev/null; then
  error "'dotnet' CLI not found. Install the .NET SDK first."
  exit 1
fi

# Initialize user-secrets stores
dotnet user-secrets init --id "$APPHOST_SECRETS_ID" --project "$REPO_ROOT/orchestration/AuthScript.AppHost" 2>/dev/null || true
dotnet user-secrets init --id "$GATEWAY_SECRETS_ID" --project "$REPO_ROOT/apps/gateway/Gateway.API" 2>/dev/null || true

# ---------------------------------------------------------------------------
# 1. Athena sandbox credentials (hardcoded — no input needed)
# ---------------------------------------------------------------------------

echo ""
echo -e "${BOLD}AuthScript Platform — Secrets Setup${RESET}"
echo ""

set_secret "athena-client-id"     "$ATHENA_CLIENT_ID"
set_secret "athena-client-secret" "$ATHENA_CLIENT_SECRET"
set_secret "athena-practice-id"   "$ATHENA_PRACTICE_ID"

# Sync to Gateway store (for standalone dev without Aspire)
dotnet user-secrets set "Athena:ClientId"     "$ATHENA_CLIENT_ID"     --id "$GATEWAY_SECRETS_ID" > /dev/null
dotnet user-secrets set "Athena:ClientSecret" "$ATHENA_CLIENT_SECRET" --id "$GATEWAY_SECRETS_ID" > /dev/null

ok "Athena sandbox credentials configured (practice ${ATHENA_PRACTICE_ID})"

# ---------------------------------------------------------------------------
# 2. LLM provider + token (defaults work out of the box)
# ---------------------------------------------------------------------------

llm_provider="${LLM_PROVIDER:-$DEFAULT_LLM_PROVIDER}"
set_secret "llm-provider" "$llm_provider"

# ---------------------------------------------------------------------------
# 3. LLM credentials (defaults to GitHub Models token)
# ---------------------------------------------------------------------------

set_secret "github-token"          "${GITHUB_TOKEN:-$GITHUB_TOKEN_DEFAULT}"
set_secret "azure-openai-key"      "${AZURE_OPENAI_API_KEY:-not-configured}"
set_secret "azure-openai-endpoint" "${AZURE_OPENAI_ENDPOINT:-not-configured}"
set_secret "google-api-key"        "${GOOGLE_API_KEY:-not-configured}"
set_secret "openai-api-key"        "${OPENAI_API_KEY:-not-configured}"
set_secret "openai-org-id"         "${OPENAI_ORG_ID:-not-configured}"

ok "LLM credentials configured (provider: ${llm_provider})"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

echo ""
echo -e "${BOLD}── Summary ──${RESET}"
echo -e "  Athena:       sandbox (practice ${ATHENA_PRACTICE_ID})"
echo -e "  LLM Provider: ${llm_provider}"
echo ""
echo -e "${DIM}Secrets stored in: ~/.microsoft/usersecrets/${APPHOST_SECRETS_ID}/secrets.json${RESET}"
echo -e "${DIM}Verify with: dotnet user-secrets list --id ${APPHOST_SECRETS_ID}${RESET}"
echo ""
ok "Setup complete. Start the platform with: dotnet run --project orchestration/AuthScript.AppHost"
