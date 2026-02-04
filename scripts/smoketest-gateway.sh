#!/usr/bin/env bash
# =============================================================================
# Gateway Smoke Test
# Tests the full workflow against Athena sandbox (Practice 195900)
# =============================================================================
set -euo pipefail

# Configuration
BASE_URL="${GATEWAY_URL:-http://localhost:5000}"
API_KEY="${API_KEY:-dev-key-123}"

# Athena sandbox test data (Practice 195900)
PATIENT_ID="a-195900.E-60178"           # Donna Sandboxtest
ENCOUNTER_ID="a-195900.encounter-62014" # Real encounter from sandbox
PROCEDURE_CODE="99213"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

# Helper for API calls
api() {
    curl -sf -H "X-API-Key: $API_KEY" "$@"
}

pass() {
    echo -e "${GREEN}✓${NC} $1"
}

fail() {
    echo -e "${RED}✗${NC} $1"
    exit 1
}

echo "=== Gateway Smoke Test ==="
echo "Base URL: $BASE_URL"
echo "Patient: $PATIENT_ID"
echo "Encounter: $ENCOUNTER_ID"
echo ""

# -----------------------------------------------------------------------------
# 1. Health check
# -----------------------------------------------------------------------------
echo -n "1. Health check... "
if curl -sf "$BASE_URL/health" > /dev/null; then
    pass "healthy"
else
    fail "health check failed"
fi

# -----------------------------------------------------------------------------
# 2. FHIR Discovery - Search patients by name
# -----------------------------------------------------------------------------
echo -n "2. Search patients by name... "
PATIENT_COUNT=$(api "$BASE_URL/api/fhir/patients?name=Sandboxtest" | jq -r '.entry | length')
if [ "$PATIENT_COUNT" -gt 0 ]; then
    pass "found $PATIENT_COUNT patients"
else
    fail "no patients found"
fi

# -----------------------------------------------------------------------------
# 3. FHIR Discovery - Get specific patient
# -----------------------------------------------------------------------------
echo -n "3. Get patient by ID... "
PATIENT_NAME=$(api "$BASE_URL/api/fhir/patients/$PATIENT_ID" | jq -r '.name[0].family')
if [ "$PATIENT_NAME" = "Sandboxtest" ]; then
    pass "$PATIENT_NAME"
else
    fail "unexpected patient name: $PATIENT_NAME"
fi

# -----------------------------------------------------------------------------
# 4. FHIR Discovery - Search encounters
# -----------------------------------------------------------------------------
echo -n "4. Search encounters... "
ENCOUNTER_COUNT=$(api "$BASE_URL/api/fhir/encounters?patientId=$PATIENT_ID" | jq -r '.entry | length')
if [ "$ENCOUNTER_COUNT" -gt 0 ]; then
    pass "found $ENCOUNTER_COUNT encounters"
else
    fail "no encounters found"
fi

# -----------------------------------------------------------------------------
# 5. Create work item with real sandbox data
# -----------------------------------------------------------------------------
echo -n "5. Create work item... "
WORK_ITEM=$(api -X POST "$BASE_URL/api/work-items" \
    -H "Content-Type: application/json" \
    -d "{\"patientId\":\"$PATIENT_ID\",\"encounterId\":\"$ENCOUNTER_ID\",\"procedureCode\":\"$PROCEDURE_CODE\"}")
WORK_ITEM_ID=$(echo "$WORK_ITEM" | jq -r '.id')
if [ -n "$WORK_ITEM_ID" ] && [ "$WORK_ITEM_ID" != "null" ]; then
    pass "$WORK_ITEM_ID"
else
    fail "failed to create work item"
fi

# -----------------------------------------------------------------------------
# 6. Rehydrate (triggers full workflow)
# -----------------------------------------------------------------------------
echo -n "6. Rehydrate work item... "
REHYDRATE_RESULT=$(api -X POST "$BASE_URL/api/work-items/$WORK_ITEM_ID/rehydrate")
NEW_STATUS=$(echo "$REHYDRATE_RESULT" | jq -r '.newStatus')
if [ -n "$NEW_STATUS" ] && [ "$NEW_STATUS" != "null" ]; then
    pass "status: $NEW_STATUS"
else
    fail "rehydrate failed"
fi

# -----------------------------------------------------------------------------
# 7. Verify work item updated
# -----------------------------------------------------------------------------
echo -n "7. Get work item... "
WORK_ITEM_STATUS=$(api "$BASE_URL/api/work-items/$WORK_ITEM_ID" | jq -r '.status')
if [ -n "$WORK_ITEM_STATUS" ]; then
    pass "status: $WORK_ITEM_STATUS"
else
    fail "failed to get work item"
fi

# -----------------------------------------------------------------------------
# 8. Clean up - Delete work item
# -----------------------------------------------------------------------------
echo -n "8. Delete work item... "
if api -X DELETE "$BASE_URL/api/work-items/$WORK_ITEM_ID" > /dev/null; then
    pass "deleted"
else
    fail "failed to delete"
fi

# -----------------------------------------------------------------------------
# 9. Verify deletion
# -----------------------------------------------------------------------------
echo -n "9. Verify deletion... "
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -H "X-API-Key: $API_KEY" "$BASE_URL/api/work-items/$WORK_ITEM_ID")
if [ "$HTTP_CODE" = "404" ]; then
    pass "confirmed (404)"
else
    fail "work item still exists (HTTP $HTTP_CODE)"
fi

echo ""
echo -e "${GREEN}=== All tests passed ===${NC}"
