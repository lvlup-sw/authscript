#!/usr/bin/env bash
# ===========================================================================
# Sync dotnet user-secrets → azd environment variables
# azd expects AZURE_ prefix for Aspire infrastructure parameters
# Run from repo root. Requires: dotnet, azd
# ===========================================================================
set -euo pipefail

APPHOST_DIR="$(cd "$(dirname "$0")/../orchestration/AuthScript.AppHost" && pwd)"

# Map: dotnet user-secret key → azd env var (AZURE_ prefixed)
declare -A SECRET_MAP=(
  ["Parameters:athena-client-id"]="AZURE_ATHENA_CLIENT_ID"
  ["Parameters:athena-client-secret"]="AZURE_ATHENA_CLIENT_SECRET"
  ["Parameters:athena-practice-id"]="AZURE_ATHENA_PRACTICE_ID"
  ["Parameters:github-token"]="AZURE_GITHUB_TOKEN"
  ["Parameters:llm-provider"]="AZURE_LLM_PROVIDER"
  ["Parameters:azure-openai-key"]="AZURE_AZURE_OPENAI_KEY"
  ["Parameters:azure-openai-endpoint"]="AZURE_AZURE_OPENAI_ENDPOINT"
  ["Parameters:google-api-key"]="AZURE_GOOGLE_API_KEY"
  ["Parameters:openai-api-key"]="AZURE_OPENAI_API_KEY"
  ["Parameters:openai-org-id"]="AZURE_OPENAI_ORG_ID"
  ["Parameters:postgres-password"]="AZURE_POSTGRES_PASSWORD"
  ["Parameters:redis-password"]="AZURE_REDIS_PASSWORD"
)

echo "Syncing dotnet user-secrets → azd env (AZURE_ prefixed)..."

while IFS=' = ' read -r key value; do
  [[ "$key" != Parameters:* ]] && continue

  azd_var="${SECRET_MAP[$key]:-}"
  [[ -z "$azd_var" ]] && continue

  if [[ "$value" == "not-configured" ]]; then
    echo "  skip: $azd_var (not-configured)"
    continue
  fi

  echo "  set:  $azd_var"
  (cd "$APPHOST_DIR" && azd env set "$azd_var" "$value" 2>/dev/null)
done < <(dotnet user-secrets list --project "$APPHOST_DIR")

echo "Done. Run 'cd $APPHOST_DIR && azd up' to deploy."
