# MVP Architecture Audit & Workstream Planning

## Summary

Audit of architecture documents against current implementation state with prioritized issue list for Gateway (.NET) and Intelligence (Python) workstreams.

**Purpose:** Reference document for creating GitHub issues on-demand with proper labels for project automation.

---

## Part 1: Architecture Audit

### MVP Demo Scope (March 11, 2026)
- **Procedure:** TBD
- **Payer:** TBD
- **CDS Hook:** `ServiceRequest.C/R/U/D`
- **Philosophy:** "Bulletproof Happy Path" - pre-validated scenarios, aggressive caching

### Component Status Matrix

| Component | Architecture Spec | Current State | Status |
|-----------|------------------|---------------|--------|
| **GATEWAY SERVICE** ||||
| CdsHookController | Receives ServiceRequest.C/R/U/D, validates JWT | NOT IMPLEMENTED | ❌ Critical Gap |
| FhirDataAggregator | Parallel FHIR fetch | COMPLETE | ✅ |
| FhirClient/FhirHttpClient | FHIR R4 with Result<T> | COMPLETE | ✅ |
| IntelligenceClient | HTTP client to Python | STUB (mock APPROVE) | ⚠️ |
| PdfFormStamper | iText7 AcroForm stamping | STUB (empty array) | ⚠️ |
| DocumentUploader | POST DocumentReference | COMPLETE | ✅ |
| DemoCacheService | Redis-backed cache | NOT IMPLEMENTED | ❌ |
| **INTELLIGENCE SERVICE** ||||
| FastAPI Application | /analyze endpoint | COMPLETE | ✅ |
| LLM Client | Multi-provider (GitHub, Azure, Gemini, OpenAI) | COMPLETE | ✅ |
| PDF Parser | Document extraction | PARTIAL (PyMuPDF, no LlamaParse) | ⚠️ |
| Evidence Extractor | LLM policy evaluation | STUB (returns MET) | ⚠️ |
| Form Generator | Recommendation + summary | STUB (returns APPROVE) | ⚠️ |
| Policy Matcher | Procedure → Policy | STUB (example policy only) | ⚠️ |
| **EPIC INTEGRATION** ||||
| OAuth JWT Validation | JWKS validation | NOT IMPLEMENTED | ❌ |
| CDS Discovery Endpoint | GET /cds-services | NOT IMPLEMENTED | ❌ |
| FHIR API Access | Bearer token auth | COMPLETE | ✅ |

### Summary
- **Gateway:** ~60% complete (infrastructure done, integrations stubbed)
- **Intelligence:** ~45% complete (infrastructure done, reasoning stubbed)
- **Epic Integration:** ~30% complete (FHIR works, CDS Hooks missing)
- **Demo Infrastructure:** 0% complete (no Redis, no cache warming)

---

## Part 2: Workstream Issues

### Label Reference
Per `.github/workflows/project-automation.yml`:
- `scope:gateway` → Gateway (.NET) workstream
- `scope:intelligence` → Intelligence (Python) workstream
- `priority:high` / `priority:low` for urgency
- `type:feature` / `type:bug` / `type:chore`

---

### GATEWAY WORKSTREAM Issues

#### GW-001: CDS Hooks Controller [P0 CRITICAL]
**Labels:** `scope:gateway`, `scope:fhir`, `priority:high`, `type:feature`

Create CDS Hooks integration for Epic:
- [ ] Discovery endpoint: `GET /cds-services/authscript`
- [ ] Hook endpoint: `POST /cds-services/authscript/ServiceRequest`
- [ ] Parse CDS Hook request (context, prefetch, fhirAuthorization)
- [ ] Build CDS Card response format
- [ ] Handle `ServiceRequest.C/R/U/D` hook events

**Files:** Create `Endpoints/CdsHooksEndpoints.cs`, `Models/CdsHook*.cs`

---

#### GW-002: JWT Validation for CDS Hooks [P0 CRITICAL]
**Labels:** `scope:gateway`, `priority:high`, `type:feature`

Validate Epic JWT signatures:
- [ ] Fetch JWKS from Epic endpoint
- [ ] Validate hook JWT signature and claims
- [ ] Cache JWKS with TTL refresh
- [ ] Extract fhirAuthorization token for API calls

**Files:** Create `Services/JwtValidator.cs`, `Configuration/JwksOptions.cs`

---

#### GW-003: IntelligenceClient HTTP Implementation [P0 CRITICAL]
**Labels:** `scope:gateway`, `scope:llm`, `priority:high`, `type:feature`

Replace stub with actual HTTP calls:
- [ ] POST clinical bundle to Intelligence `/analyze` endpoint
- [ ] Deserialize PAFormResponse
- [ ] Handle timeout/error scenarios
- [ ] Add resilience with Polly

**Files:** Modify `Services/IntelligenceClient.cs`

---

#### GW-004: PdfFormStamper iText Implementation [P0 CRITICAL]
**Labels:** `scope:gateway`, `scope:pdf`, `priority:high`, `type:feature`

Implement PDF form stamping:
- [ ] Load PA form template from assets (procedure-specific)
- [ ] Map PAFormData.FieldMappings to AcroForm fields
- [ ] Flatten form after filling
- [ ] Return stamped PDF bytes

**Files:** Modify `Services/PdfFormStamper.cs`, add PA form template to `Assets/`

---

#### GW-005: Redis Caching Integration [P1]
**Labels:** `scope:gateway`, `priority:low`, `type:feature`

Add demo response caching:
- [ ] Add Aspire Redis dependency
- [ ] Implement IAnalysisResultStore with Redis backend
- [ ] Cache key pattern: `authscript:demo:{patient_id}:{procedure_code}`
- [ ] TTL: 24 hours for demo stability

**Files:** Modify `Services/AnalysisResultStore.cs`, update `Program.cs`

---

### INTELLIGENCE WORKSTREAM Issues

#### INT-001: Evidence Extraction with LLM [P0 CRITICAL]
**Labels:** `scope:intelligence`, `scope:llm`, `priority:high`, `type:feature`

Implement policy-based evidence extraction:
- [ ] Create prompt template for evidence extraction
- [ ] Call LLM via `llm_client.chat_completion()`
- [ ] Parse structured JSON response (criterion_id, status, evidence, confidence)
- [ ] Map to EvidenceItem list
- [ ] Handle partial matches (MET, NOT_MET, UNCLEAR)

**Files:** Modify `reasoning/evidence_extractor.py`

---

#### INT-002: Form Generation with LLM [P0 CRITICAL]
**Labels:** `scope:intelligence`, `scope:llm`, `priority:high`, `type:feature`

Generate PA form data from evidence:
- [ ] Create prompt template for clinical summary
- [ ] Calculate recommendation based on evidence (APPROVE/NEED_INFO/MANUAL_REVIEW)
- [ ] Generate field_mappings from clinical bundle + policy
- [ ] Return complete PAFormResponse

**Files:** Modify `reasoning/form_generator.py`

---

#### INT-003: Policy Matching Implementation [P0 CRITICAL]
**Labels:** `scope:intelligence`, `priority:high`, `type:feature`

Match procedures to payer policies:
- [ ] Create policy definition for demo procedure/payer combination
- [ ] Match CPT codes to policy
- [ ] Load policy criteria and field mappings
- [ ] Return policy for evidence extraction

**Files:** Create policy module in `policies/`, modify `api/analyze.py`

---

#### INT-004: LlamaParse Integration [P1]
**Labels:** `scope:intelligence`, `scope:pdf`, `priority:low`, `type:feature`

Add LlamaParse for better document extraction:
- [ ] Add LlamaParse API client
- [ ] Fallback to PyMuPDF4LLM on error
- [ ] Process documents in `/analyze/with-documents`
- [ ] Include extracted text in evidence context

**Files:** Modify `parsers/pdf_parser.py`, add `parsers/llamaparse_client.py`

---

#### INT-005: Wire Analysis Endpoint [P0 CRITICAL]
**Labels:** `scope:intelligence`, `priority:high`, `type:feature`

Connect all components in analyze endpoint:
- [ ] Load policy based on procedure_code
- [ ] Call evidence_extractor with clinical bundle + policy
- [ ] Call form_generator with evidence + policy
- [ ] Process documents if provided
- [ ] Return complete PAFormResponse

**Files:** Modify `api/analyze.py`

---

### CROSS-CUTTING Issues

#### XC-001: End-to-End Integration Test [P1]
**Labels:** `scope:gateway`, `scope:intelligence`, `priority:high`, `type:feature`

Test full pipeline:
- [ ] Create integration test project
- [ ] Mock Epic FHIR responses with Synthea data
- [ ] Verify CDS Hook → FHIR → Intelligence → PDF → Upload flow
- [ ] Assert CDS Card response format

---

#### XC-002: Demo Patient Data [P1]
**Labels:** `scope:fhir`, `priority:low`, `type:chore`

Generate demo patients with Synthea:
- [ ] demo-001: Perfect candidate for demo procedure (all criteria MET)
- [ ] demo-002: Missing criteria (edge case scenario)
- [ ] Store as FHIR bundles in `test-data/`

---

#### XC-003: Cache Warming Script [P1]
**Labels:** `scope:gateway`, `scope:intelligence`, `priority:low`, `type:chore`

Pre-compute demo responses:
- [ ] Create `scripts/warm_demo_cache.py`
- [ ] Run analysis for demo patients
- [ ] Store in Redis cache
- [ ] Run as part of demo setup

---

## Part 3: Priority Order for MVP

### Week 1: Core Reasoning
| Issue | Workstream | Description |
|-------|------------|-------------|
| INT-001 | Intelligence | Evidence Extraction with LLM |
| INT-002 | Intelligence | Form Generation with LLM |
| INT-003 | Intelligence | Policy Matching |
| INT-005 | Intelligence | Wire Analysis Endpoint |

### Week 2: Epic Integration
| Issue | Workstream | Description |
|-------|------------|-------------|
| GW-001 | Gateway | CDS Hooks Controller |
| GW-002 | Gateway | JWT Validation |
| GW-003 | Gateway | IntelligenceClient HTTP |

### Week 3: PDF & Polish
| Issue | Workstream | Description |
|-------|------------|-------------|
| GW-004 | Gateway | PdfFormStamper iText |
| XC-001 | Both | End-to-End Integration Test |
| XC-002 | Both | Demo Patient Data |

### Week 4: Demo Reliability
| Issue | Workstream | Description |
|-------|------------|-------------|
| GW-005 | Gateway | Redis Caching |
| XC-003 | Both | Cache Warming Script |
| INT-004 | Intelligence | LlamaParse (stretch) |

---

## Part 4: Architecture Doc Updates Needed

1. **Section 11.3 Status Table** - Mark FHIR data fetching as COMPLETE (done in refactor)
2. **Add note** - CDS Hooks JWT ≠ OAuth token (different auth mechanisms)
3. **Update policy reference** - `example_policy.py` should become demo-specific policy module

---

## Verification

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

# 4. Verify Dashboard shows live status
open http://localhost:3000
```

---

## Critical Files

| File | Purpose |
|------|---------|
| `docs/designs/2025-01-21-authscript-demo-architecture.md` | Source of truth |
| `apps/gateway/Gateway.API/Services/IntelligenceClient.cs` | STUB → HTTP |
| `apps/gateway/Gateway.API/Services/PdfFormStamper.cs` | STUB → iText |
| `apps/intelligence/src/reasoning/evidence_extractor.py` | STUB → LLM |
| `apps/intelligence/src/reasoning/form_generator.py` | STUB → LLM |
| `apps/intelligence/src/api/analyze.py` | Wire components |
