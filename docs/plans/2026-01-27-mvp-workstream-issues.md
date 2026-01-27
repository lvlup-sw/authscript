# MVP Architecture Audit & Workstream Planning

## Summary

Audit of architecture documents against current implementation state with prioritized issue list for Gateway (.NET) and Intelligence (Python) workstreams.

**Purpose:** Reference document for creating GitHub issues. Issues should follow the comprehensive format defined in `.github/ISSUE_TEMPLATE/implementation.yml`.

---

## Part 1: Architecture Audit

### MVP Demo Scope (March 11, 2026)
- **Procedure:** TBD (see #5)
- **Payer:** TBD (see #5)
- **CDS Hook:** `ServiceRequest.C/R/U/D`
- **Philosophy:** "Bulletproof Happy Path" - pre-validated scenarios, aggressive caching

### Component Status Matrix

| Component | Architecture Spec | Current State | Status |
|-----------|------------------|---------------|--------|
| **GATEWAY SERVICE** ||||
| CdsHookController | Receives ServiceRequest.C/R/U/D, validates JWT | NOT IMPLEMENTED | ❌ Critical Gap |
| FhirDataAggregator | Parallel FHIR fetch | COMPLETE | ✅ |
| FhirClient/FhirHttpClient | FHIR R4 with Result<T> | COMPLETE (needs validation) | ⚠️ #12 |
| IntelligenceClient | HTTP client to Python | STUB (mock APPROVE) | ⚠️ #10 |
| PdfFormStamper | iText7 AcroForm stamping | STUB (empty array) | ⚠️ #11 |
| DocumentUploader | POST DocumentReference | COMPLETE | ✅ |
| DemoCacheService | Redis-backed cache | NOT IMPLEMENTED | ❌ |
| **INTELLIGENCE SERVICE** ||||
| FastAPI Application | /analyze endpoint | COMPLETE | ✅ |
| LLM Client | Multi-provider (GitHub, Azure, Gemini, OpenAI) | COMPLETE | ✅ |
| PDF Parser | Document extraction | COMPLETE (PyMuPDF4LLM) | ✅ |
| Evidence Extractor | LLM policy evaluation | STUB (returns MET) | ⚠️ #6 |
| Form Generator | Recommendation + summary | STUB (returns APPROVE) | ⚠️ #7 |
| Policy Matcher | Procedure → Policy | STUB (example policy only) | ⚠️ #8 |
| **EPIC INTEGRATION** ||||
| OAuth JWT Validation | JWKS validation | NOT IMPLEMENTED | ❌ |
| CDS Discovery Endpoint | GET /cds-services | NOT IMPLEMENTED | ❌ |
| FHIR API Access | Bearer token auth | COMPLETE | ✅ |

### Summary
- **Gateway:** ~60% complete (infrastructure done, integrations stubbed)
- **Intelligence:** ~50% complete (infrastructure done, reasoning stubbed)
- **Epic Integration:** ~30% complete (FHIR works, CDS Hooks missing)
- **Demo Infrastructure:** 0% complete (no Redis, no cache warming)

---

## Part 2: Issue Format Guidelines

### Required Sections for Implementation Issues

Per `.github/ISSUE_TEMPLATE/implementation.yml`:

| Section | Purpose |
|---------|---------|
| **Summary** | What needs to be implemented (1-2 sentences) |
| **Context** | Where it fits, inputs/outputs, business logic |
| **Dependencies** | Table of blocking/blocked issues with #N links |
| **Tasks** | Phased checklist (Core → Error Handling → Testing) |
| **Files** | Table with Action column (Modify/Create/Reference) |
| **Design References** | Multiple full GitHub URLs to relevant sections |
| **Acceptance Criteria** | Specific, testable outcomes |

### Label Reference

Per `.github/workflows/project-automation.yml`:
- `scope:gateway` → Gateway (.NET) workstream
- `scope:intelligence` → Intelligence (Python) workstream
- `priority:high` / `priority:low` for urgency
- `type:feature` / `type:bug` / `type:chore`

---

## Part 3: Existing Issues (Created)

These issues have been created with comprehensive context:

| Issue | Title | Workstream | Status |
|-------|-------|------------|--------|
| #5 | Demo procedure/payer selection | Cross-cutting | Open |
| #6 | Evidence extraction with LLM | Intelligence | Open |
| #7 | Form generation with LLM | Intelligence | Open |
| #8 | Policy matching | Intelligence | Open |
| #9 | Wire analysis endpoint | Intelligence | Open |
| #10 | IntelligenceClient HTTP | Gateway | Open |
| #11 | PDF form stamping with iText | Gateway | Open |
| #12 | FHIR sandbox validation | Gateway | Open |

---

## Part 4: Remaining Issues to Create

### GATEWAY WORKSTREAM

#### GW-001: CDS Hooks Controller [P0 CRITICAL]
**Labels:** `scope:gateway`, `scope:fhir`, `priority:high`, `type:feature`

**Summary:** Create CDS Hooks integration endpoints for Epic ServiceRequest.C/R/U/D hooks.

**Context:**
- Discovery endpoint returns service metadata for Epic registration
- Hook endpoint receives clinical context and returns CDS Cards
- Must parse prefetch data and fhirAuthorization token

**Dependencies:**
| Issue | Relationship |
|-------|--------------|
| GW-002 | Blocked by - needs JWT validation |
| #10 | Blocks - CDS controller calls IntelligenceClient |

**Tasks:**
- [ ] Discovery endpoint: `GET /cds-services/authscript`
- [ ] Hook endpoint: `POST /cds-services/authscript/ServiceRequest`
- [ ] Parse CDS Hook request (context, prefetch, fhirAuthorization)
- [ ] Build CDS Card response format
- [ ] Handle `ServiceRequest.C/R/U/D` hook events

**Files:**
| File | Action |
|------|--------|
| `Endpoints/CdsHooksEndpoints.cs` | Create |
| `Models/CdsHookRequest.cs` | Create |
| `Models/CdsCard.cs` | Create |

**Design References:**
- [§3.1 Registration & Configuration](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#31-registration--configuration)
- [§3.2 Hook Selection](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#32-hook-selection-servicerequest-crud)

---

#### GW-002: JWT Validation for CDS Hooks [P0 CRITICAL]
**Labels:** `scope:gateway`, `priority:high`, `type:feature`

**Summary:** Validate Epic JWT signatures on CDS Hook requests using JWKS.

**Context:**
- Epic signs CDS Hook requests with JWT
- Must validate signature against Epic's JWKS endpoint
- Extract fhirAuthorization.access_token for subsequent FHIR calls

**Dependencies:**
| Issue | Relationship |
|-------|--------------|
| GW-001 | Unblocks - CDS controller needs JWT validation |

**Tasks:**
- [ ] Fetch JWKS from Epic endpoint (with caching)
- [ ] Validate JWT signature and claims (iss, aud, exp)
- [ ] Cache JWKS with TTL refresh
- [ ] Extract fhirAuthorization token for API calls

**Files:**
| File | Action |
|------|--------|
| `Services/JwtValidator.cs` | Create |
| `Configuration/JwksOptions.cs` | Create |

**Design References:**
- [§3.3 Authentication Flow](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#33-authentication-flow)
- [Appendix A: Epic URLs](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#a-epic-sandbox-urls)

---

#### GW-005: Redis Caching Integration [P1]
**Labels:** `scope:gateway`, `priority:low`, `type:feature`

**Summary:** Add Redis-backed caching for demo response speedup.

**Context:**
- Pre-computed responses served in <100ms for demo reliability
- Cache key pattern: `authscript:demo:{patient_id}:{procedure_code}`
- 24-hour TTL for demo stability

**Dependencies:**
| Issue | Relationship |
|-------|--------------|
| XC-003 | Unblocks - cache warming script needs this |

**Tasks:**
- [ ] Add Aspire Redis dependency
- [ ] Implement IAnalysisResultStore with Redis backend
- [ ] Configure cache key pattern and TTL
- [ ] Add cache check in CDS Hook controller

**Files:**
| File | Action |
|------|--------|
| `Services/AnalysisResultStore.cs` | Modify |
| `Program.cs` | Modify - add Redis |

**Design References:**
- [§2.4 Redis Cache](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#24-redis-cache)
- [§6.1 Bulletproof Philosophy](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#61-the-bulletproof-philosophy)

---

### CROSS-CUTTING Issues

#### XC-001: End-to-End Integration Test [P1]
**Labels:** `scope:gateway`, `scope:intelligence`, `priority:high`, `type:feature`

**Summary:** Create integration test verifying full CDS Hook → PDF upload pipeline.

**Context:**
- Tests full flow with mocked Epic FHIR responses
- Uses Synthea-generated test data
- Validates CDS Card response format

**Dependencies:**
| Issue | Relationship |
|-------|--------------|
| GW-001 | Blocked by - needs CDS Hook controller |
| #9 | Blocked by - needs wired analysis endpoint |

**Tasks:**
- [ ] Create integration test project
- [ ] Mock Epic FHIR responses with Synthea data
- [ ] Verify CDS Hook → FHIR → Intelligence → PDF → Upload flow
- [ ] Assert CDS Card response format

**Design References:**
- [§1.2 Request Flow Sequence](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#12-request-flow-sequence)

---

#### XC-002: Demo Patient Data [P1]
**Labels:** `scope:fhir`, `priority:low`, `type:chore`

**Summary:** Generate Synthea patients for demo scenarios.

**Context:**
- demo-001: Perfect candidate (all criteria MET) - guaranteed happy path
- demo-002 through demo-005: Edge cases per §5.1

**Dependencies:**
| Issue | Relationship |
|-------|--------------|
| #5 | Blocked by - need procedure/payer to define scenarios |

**Tasks:**
- [ ] Configure Synthea for demo procedure conditions
- [ ] Generate demo-001 through demo-005 patients
- [ ] Store as FHIR bundles in `test-data/`
- [ ] Document each scenario's expected outcome

**Design References:**
- [§5.1 Synthetic Patient Generation](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#51-synthetic-patient-generation)

---

#### XC-003: Cache Warming Script [P1]
**Labels:** `scope:gateway`, `scope:intelligence`, `priority:low`, `type:chore`

**Summary:** Pre-compute demo responses for guaranteed fast demo.

**Context:**
- Run night before demo to populate Redis cache
- Ensures <100ms response for demo patients

**Dependencies:**
| Issue | Relationship |
|-------|--------------|
| GW-005 | Blocked by - needs Redis integration |
| XC-002 | Blocked by - needs demo patient data |

**Tasks:**
- [ ] Create `scripts/warm_demo_cache.py`
- [ ] Run full analysis pipeline for each demo patient
- [ ] Store results in Redis with demo key pattern
- [ ] Add to demo day checklist

**Design References:**
- [§5.2 Pre-Computation Strategy](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#52-pre-computation-strategy)
- [§6.4 Demo Day Checklist](https://github.com/lvlup-sw/authscript/blob/main/docs/designs/2025-01-21-authscript-demo-architecture.md#64-demo-day-checklist)

---

## Part 5: Priority Order for MVP

### Week 1: Core Reasoning (Intelligence)
| Issue | Description |
|-------|-------------|
| #5 | Demo procedure/payer selection (UNBLOCKS ALL) |
| #6 | Evidence Extraction with LLM |
| #7 | Form Generation with LLM |
| #8 | Policy Matching |
| #9 | Wire Analysis Endpoint |

### Week 2: Epic Integration (Gateway)
| Issue | Description |
|-------|-------------|
| GW-001 | CDS Hooks Controller |
| GW-002 | JWT Validation |
| #10 | IntelligenceClient HTTP |
| #12 | FHIR Sandbox Validation |

### Week 3: PDF & Polish
| Issue | Description |
|-------|-------------|
| #11 | PdfFormStamper iText |
| XC-001 | End-to-End Integration Test |
| XC-002 | Demo Patient Data |

### Week 4: Demo Reliability
| Issue | Description |
|-------|-------------|
| GW-005 | Redis Caching |
| XC-003 | Cache Warming Script |

---

## Part 6: Verification

After implementation:
```bash
# 1. Run all tests
dotnet test apps/gateway/Gateway.API.Tests
cd apps/intelligence && uv run pytest

# 2. Start services with Aspire
cd orchestration/AuthScript.AppHost && dotnet run

# 3. Test CDS Hook manually
curl -X POST http://localhost:5000/cds-services/authscript/ServiceRequest \
  -H "Content-Type: application/json" \
  -d @test-data/cds-hook-request.json

# 4. Verify response format
# Should return CDS Card with "PA Form Ready" or similar
```

---

## Critical Files Reference

| File | Purpose | Issue |
|------|---------|-------|
| `apps/gateway/Gateway.API/Services/IntelligenceClient.cs` | STUB → HTTP | #10 |
| `apps/gateway/Gateway.API/Services/PdfFormStamper.cs` | STUB → iText | #11 |
| `apps/gateway/Gateway.API/Services/FhirClient.cs` | Validate against Epic | #12 |
| `apps/intelligence/src/reasoning/evidence_extractor.py` | STUB → LLM | #6 |
| `apps/intelligence/src/reasoning/form_generator.py` | STUB → LLM | #7 |
| `apps/intelligence/src/policies/` | Example → Demo policy | #8 |
| `apps/intelligence/src/api/analyze.py` | Wire components | #9 |
