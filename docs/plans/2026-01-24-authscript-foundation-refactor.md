# AuthScript Foundation Refactor & Implementation Plan

**Feature:** authscript-demo-architecture
**Date:** 2026-01-24
**Design Doc:** [docs/designs/2025-01-21-authscript-demo-architecture.md](../designs/2025-01-21-authscript-demo-architecture.md)
**Status:** Draft

---

## Executive Summary

This plan addresses two parallel concerns:

1. **Foundation Refactor**: The code ported from `ares-elite-platform` is incomplete and disorganized. Key infrastructure patterns are missing or incomplete.
2. **Implementation Gaps**: The AuthScript demo architecture design has components that are partially implemented with stubs.

The plan follows strict TDD and organizes work into parallelizable task groups.

---

## Code Comparison: What's Missing

### Ported from ares-elite-platform (incomplete)

| Component | Ares Elite | Prior Auth | Gap |
|-----------|------------|------------|-----|
| **shared/types** | Modular: users.ts, fhir.ts, common.ts, generated/ | Flat: single index.ts with all types | Missing: common patterns (pagination, API response wrappers), modular organization |
| **shared/validation** | Zod schemas: IsoDateTimeSchema, PaginationQuerySchema, UuidSchema, etc. | Basic schemas only | Missing: ISO datetime, pagination, UUID, non-empty string schemas |
| **utils/validationUtils.ts** | Full: isValidString, hasNonEmptyFields, isValidEmail, hasRequiredFields | Not ported | Entirely missing |
| **utils/typeConverters.ts** | Full: parseDouble, parseInt32, parsePercentage, parseTimeToSeconds, hasValue | Not ported | Entirely missing |
| **config/secrets.ts** | SecretsManager with validation, ENV_VAR_MAPPINGS | Not present | Missing env var management |
| **api/customFetch.ts** | Integrated with BFF, credential handling, detailed error parsing | Basic implementation | Missing: traceId extraction, detailed error parsing |
| **Service pattern** | Centralized with guards + schema validation | Basic fetch wrappers | Missing: input validation, schema-validated responses |
| **useFormValidation** | TanStack Form + Zod integration | Not ported | Entirely missing |
| **Container components** | ScrollPage, Container, FullScreenContainer | Not present | Missing layout primitives |
| **Test setup** | Vitest config, fixtures, component tests | Configured but empty | No tests written |

### Dashboard Components (stubbed)

| Component | Status | Gap |
|-----------|--------|-----|
| StatusFeed | Returns empty array | No real-time updates, no SignalR/polling |
| EvidencePanel | Returns placeholder | Needs API integration |
| ConfidenceMeter | Returns 0 | Needs API integration |
| FormPreview | Static placeholder | Needs PDF preview logic |
| PolicyChecklist | Static MRI lumbar criteria | Needs dynamic criteria from policy |
| Analysis page | Basic layout | Needs real data fetching |

### Backend Services (partially implemented)

| Component | Status | Gap |
|-----------|--------|-----|
| CdsHooksEndpoints | ~95% | Mostly complete |
| AnalysisEndpoints | Stubs | Need full implementation |
| EpicFhirClient | Complete | - |
| IntelligenceClient | Complete | - |
| PdfFormStamper | 80% | Template may be missing |
| EpicUploader | 60% | Needs OAuth token handling |
| Intelligence evidence_extractor.py | Complete | - |
| Intelligence form_generator.py | Referenced | Need to verify |

---

## Implementation Strategy

### Phase 1: Foundation Infrastructure (Parallel)

Establish missing infrastructure before building features. These tasks are independent.

### Phase 2: Shared Module Expansion (Sequential)

Expand shared types and validation in proper order.

### Phase 3: Dashboard Integration (Parallel)

Wire up components to real APIs.

### Phase 4: Backend Completion (Parallel)

Complete stubbed endpoints and services.

### Phase 5: End-to-End Integration (Sequential)

Wire everything together with proper error handling.

---

## Task Breakdown

### Group A: Foundation Infrastructure (Parallelizable)

---

#### Task 001: Add validation utility functions
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `isValidString_EmptyString_ReturnsFalse`
   - File: `apps/dashboard/src/utils/__tests__/validationUtils.test.ts`
   - Expected failure: Module not found

2. **[GREEN]** Implement validation utilities
   - File: `apps/dashboard/src/utils/validationUtils.ts`
   - Functions: `isValidString`, `isValidNumber`, `hasNonEmptyFields`, `isNonEmptyArray`, `isValidEmail`, `isValidDateString`, `hasRequiredFields`, `allFieldsValid`

3. **[REFACTOR]** Add JSDoc comments

**Dependencies:** None
**Branch:** `feature/001-validation-utils`

---

#### Task 002: Add type converter utilities
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `parseDouble_ValidString_ReturnsNumber`
   - File: `apps/dashboard/src/utils/__tests__/typeConverters.test.ts`
   - Expected failure: Module not found

2. **[GREEN]** Implement type converters
   - File: `apps/dashboard/src/utils/typeConverters.ts`
   - Functions: `parseDouble`, `parseInt32`, `parsePercentage`, `parseTimeToSeconds`, `parseTimeToMilliseconds`, `hasValue`

3. **[REFACTOR]** Add JSDoc comments

**Dependencies:** None
**Branch:** `feature/002-type-converters`

---

#### Task 003: Add SecretsManager for environment configuration
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `SecretsManager_MissingRequiredVar_ThrowsInProduction`
   - File: `apps/dashboard/src/config/__tests__/secrets.test.ts`
   - Expected failure: Module not found

2. **[GREEN]** Implement SecretsManager
   - File: `apps/dashboard/src/config/secrets.ts`
   - Features: ENV_VAR_MAPPINGS, validation, getters, getConfigSummary

3. **[REFACTOR]** Add production vs development behavior

**Dependencies:** None
**Branch:** `feature/003-secrets-manager`

---

#### Task 004: Add Container layout components
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `Container_RendersChildren_WithDefaultStyles`
   - File: `apps/dashboard/src/components/ui/__tests__/Containers.test.tsx`
   - Expected failure: Module not found

2. **[GREEN]** Implement container components
   - File: `apps/dashboard/src/components/ui/Containers.tsx`
   - Components: `Container`, `ScrollContainer`, `ScrollPage`, `FullScreenContainer`, `HeadlessScroll`

3. **[REFACTOR]** Extract shared Tailwind patterns

**Dependencies:** None
**Branch:** `feature/004-container-components`

---

### Group B: Shared Module Expansion (Sequential)

---

#### Task 005: Refactor shared/types into modular structure
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** No (foundation for 006-008)

1. **[RED]** Write test: `CommonTypes_PaginatedResponse_HasRequiredFields`
   - File: `shared/types/src/__tests__/common.test.ts`
   - Expected failure: Module not found

2. **[GREEN]** Create modular type structure
   - Files:
     - `shared/types/src/common.ts` - SortDirection, PaginationParams, PaginatedResponse, ApiError, ApiResponse
     - `shared/types/src/authscript.ts` - Move PA types here
     - `shared/types/src/cds.ts` - Move CDS types here
     - `shared/types/src/index.ts` - Re-export all modules

3. **[REFACTOR]** Add package.json multi-export

**Dependencies:** None
**Branch:** `feature/005-types-modular`

---

#### Task 006: Expand shared/validation with common schemas
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** No (depends on 005)

1. **[RED]** Write test: `IsoDateTimeSchema_InvalidFormat_Fails`
   - File: `shared/validation/src/__tests__/common.test.ts`
   - Expected failure: Schema not found

2. **[GREEN]** Add common validation schemas
   - File: `shared/validation/src/common.ts`
   - Schemas: `IsoDateTimeSchema`, `IsoDateSchema`, `SortDirectionSchema`, `PaginationQuerySchema`, `NonEmptyStringSchema`, `UuidSchema`

3. **[REFACTOR]** Modular structure with index re-exports

**Dependencies:** Task 005
**Branch:** `feature/006-validation-common`

---

### Group C: API Layer Enhancement (Parallelizable after Group B)

---

#### Task 007: Enhance customFetch with proper error handling
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes (after 005)

1. **[RED]** Write test: `customFetch_Non2xxResponse_ThrowsApiError`
   - File: `apps/dashboard/src/api/__tests__/customFetch.test.ts`
   - Expected failure: Missing error details

2. **[GREEN]** Enhance customFetch
   - File: `apps/dashboard/src/api/customFetch.ts`
   - Features: traceId extraction, detailed error parsing, base URL from config

3. **[REFACTOR]** Use SecretsManager for base URL

**Dependencies:** Task 003, Task 005
**Branch:** `feature/007-custom-fetch-enhance`

---

#### Task 008: Refactor authscriptService with service pattern
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes (after 005, 006)

1. **[RED]** Write test: `authscriptService_triggerAnalysis_ValidatesInput`
   - File: `apps/dashboard/src/api/__tests__/authscriptService.test.ts`
   - Expected failure: No validation

2. **[GREEN]** Refactor to service pattern
   - File: `apps/dashboard/src/api/authscriptService.ts`
   - Features: Guard clauses, input validation, schema-validated responses, use shared types

3. **[REFACTOR]** Extract common request logic

**Dependencies:** Task 005, Task 006
**Branch:** `feature/008-authscript-service`

---

### Group D: Dashboard Component Integration (Parallelizable)

---

#### Task 009: Wire StatusFeed to real-time updates
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `StatusFeed_ReceivesUpdate_DisplaysStep`
   - File: `apps/dashboard/src/components/__tests__/StatusFeed.test.tsx`
   - Expected failure: Returns empty

2. **[GREEN]** Implement polling/SSE for status
   - File: `apps/dashboard/src/components/StatusFeed.tsx`
   - Features: Poll /status endpoint, display steps, show progress

3. **[REFACTOR]** Extract useStatusPolling hook

**Dependencies:** Task 008
**Branch:** `feature/009-status-feed`

---

#### Task 010: Wire EvidencePanel to API data
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `EvidencePanel_WithEvidence_DisplaysItems`
   - File: `apps/dashboard/src/components/__tests__/EvidencePanel.test.tsx`
   - Expected failure: Returns placeholder

2. **[GREEN]** Integrate with analysis response
   - File: `apps/dashboard/src/components/EvidencePanel.tsx`
   - Features: Accept evidence prop, display status badges, show sources

3. **[REFACTOR]** Extract EvidenceItem subcomponent

**Dependencies:** Task 005
**Branch:** `feature/010-evidence-panel`

---

#### Task 011: Wire ConfidenceMeter to API data
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `ConfidenceMeter_HighConfidence_ShowsGreen`
   - File: `apps/dashboard/src/components/__tests__/ConfidenceMeter.test.tsx`
   - Expected failure: Always 0

2. **[GREEN]** Integrate with analysis response
   - File: `apps/dashboard/src/components/ConfidenceMeter.tsx`
   - Features: Accept score prop, visual indicator, color coding

3. **[REFACTOR]** Add accessibility labels

**Dependencies:** None
**Branch:** `feature/011-confidence-meter`

---

#### Task 012: Implement FormPreview with PDF display
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `FormPreview_WithFieldMappings_HighlightsFields`
   - File: `apps/dashboard/src/components/__tests__/FormPreview.test.tsx`
   - Expected failure: Static placeholder

2. **[GREEN]** Implement form preview
   - File: `apps/dashboard/src/components/FormPreview.tsx`
   - Features: Display field mappings, side-by-side comparison, download button

3. **[REFACTOR]** Add loading states

**Dependencies:** Task 008
**Branch:** `feature/012-form-preview`

---

#### Task 013: Wire Analysis page to real data
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** No (depends on 009-012)

1. **[RED]** Write test: `AnalysisPage_ValidTransactionId_FetchesData`
   - File: `apps/dashboard/src/routes/__tests__/analysis.test.tsx`
   - Expected failure: Static data

2. **[GREEN]** Integrate with API
   - File: `apps/dashboard/src/routes/analysis.$transactionId.tsx`
   - Features: Fetch analysis, pass props to components, handle loading/error

3. **[REFACTOR]** Add React Query for caching

**Dependencies:** Tasks 009-012
**Branch:** `feature/013-analysis-page`

---

### Group E: Backend Completion (Parallelizable)

---

#### Task 014: Implement AnalysisEndpoints GET handler
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `GetAnalysis_ValidId_ReturnsAnalysis`
   - File: `apps/gateway/tests/Gateway.API.Tests/Endpoints/AnalysisEndpointsTests.cs`
   - Expected failure: Returns placeholder

2. **[GREEN]** Implement GET /api/analysis/{transactionId}
   - File: `apps/gateway/src/Gateway.API/Endpoints/AnalysisEndpoints.cs`
   - Features: Fetch from cache/DB, return full analysis

3. **[REFACTOR]** Add proper error responses

**Dependencies:** None
**Branch:** `feature/014-analysis-get`

---

#### Task 015: Implement AnalysisEndpoints status handler
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `GetStatus_InProgress_ReturnsCurrentStep`
   - File: `apps/gateway/tests/Gateway.API.Tests/Endpoints/AnalysisEndpointsTests.cs`
   - Expected failure: Returns pending

2. **[GREEN]** Implement GET /api/analysis/{transactionId}/status
   - File: `apps/gateway/src/Gateway.API/Endpoints/AnalysisEndpoints.cs`
   - Features: Track processing steps, return current progress

3. **[REFACTOR]** Add status persistence

**Dependencies:** None
**Branch:** `feature/015-analysis-status`

---

#### Task 016: Implement AnalysisEndpoints form download
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `GetForm_CompletedAnalysis_ReturnsPdf`
   - File: `apps/gateway/tests/Gateway.API.Tests/Endpoints/AnalysisEndpointsTests.cs`
   - Expected failure: Not implemented

2. **[GREEN]** Implement GET /api/analysis/{transactionId}/form
   - File: `apps/gateway/src/Gateway.API/Endpoints/AnalysisEndpoints.cs`
   - Features: Retrieve stamped PDF, return as blob

3. **[REFACTOR]** Add caching headers

**Dependencies:** None
**Branch:** `feature/016-analysis-form`

---

#### Task 017: Implement AnalysisEndpoints submit handler
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** Yes

1. **[RED]** Write test: `Submit_ValidAnalysis_UploadsToEpic`
   - File: `apps/gateway/tests/Gateway.API.Tests/Endpoints/AnalysisEndpointsTests.cs`
   - Expected failure: Not implemented

2. **[GREEN]** Implement POST /api/analysis/{transactionId}/submit
   - File: `apps/gateway/src/Gateway.API/Endpoints/AnalysisEndpoints.cs`
   - Features: Call EpicUploader, return document ID

3. **[REFACTOR]** Add idempotency check

**Dependencies:** None
**Branch:** `feature/017-analysis-submit`

---

### Group F: End-to-End Integration (Sequential)

---

#### Task 018: Verify Intelligence form_generator implementation
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** No

1. **[RED]** Write test: `form_generator_ValidEvidence_ReturnsFormData`
   - File: `apps/intelligence/tests/test_form_generator.py`
   - Expected failure: Verify behavior

2. **[GREEN]** Implement/verify form generator
   - File: `apps/intelligence/src/reasoning/form_generator.py`
   - Features: Generate field_mappings from evidence

3. **[REFACTOR]** Add output validation

**Dependencies:** None
**Branch:** `feature/018-form-generator`

---

#### Task 019: Add test PDF template
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** No

1. **[RED]** Verify PDF template exists
   - Check: `assets/pdf-templates/mri-lumbar-pa-form.pdf`
   - Expected: File missing or wrong format

2. **[GREEN]** Add template with AcroForm fields
   - File: `assets/pdf-templates/mri-lumbar-pa-form.pdf`
   - Fields: patient_name, patient_dob, member_id, diagnosis_codes, procedure_code, clinical_summary

3. **[REFACTOR]** Document field names

**Dependencies:** None
**Branch:** `feature/019-pdf-template`

---

#### Task 020: End-to-end integration test
**Phase:** RED → GREEN → REFACTOR
**Parallelizable:** No (final task)

1. **[RED]** Write test: `E2E_OrderSelect_ReturnsFilledForm`
   - File: Manual test script
   - Expected failure: Integration issues

2. **[GREEN]** Fix integration issues found
   - Verify: CDS Hook → Gateway → Intelligence → Gateway → Response

3. **[REFACTOR]** Add demo cache warming script

**Dependencies:** Tasks 001-019
**Branch:** `feature/020-e2e-integration`

---

## Parallelization Summary

```
Phase 1 (Parallel):
├── Task 001: validation utils
├── Task 002: type converters
├── Task 003: secrets manager
└── Task 004: container components

Phase 2 (Sequential):
├── Task 005: types modular (depends: none)
└── Task 006: validation common (depends: 005)

Phase 3 (Parallel, after Phase 2):
├── Task 007: custom fetch (depends: 003, 005)
├── Task 008: authscript service (depends: 005, 006)
├── Task 009: status feed (depends: 008)
├── Task 010: evidence panel (depends: 005)
├── Task 011: confidence meter (depends: none)
└── Task 012: form preview (depends: 008)

Phase 4 (Parallel):
├── Task 013: analysis page (depends: 009-012)
├── Task 014: analysis GET
├── Task 015: analysis status
├── Task 016: analysis form
├── Task 017: analysis submit
└── Task 018: form generator

Phase 5 (Sequential):
├── Task 019: PDF template
└── Task 020: E2E integration (depends: all)
```

---

## Success Criteria

- [ ] All shared types use modular structure with multi-export
- [ ] All validation schemas match ares-elite-platform patterns
- [ ] Dashboard components display real API data
- [ ] Analysis endpoints return actual results (not stubs)
- [ ] CDS Hook → Form submission flow works end-to-end
- [ ] Test coverage: >80% for new code
- [ ] No `any` types in TypeScript
- [ ] All utilities have JSDoc documentation

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| PDF template format issues | Test with iText7 early (Task 019) |
| Intelligence API contract mismatch | Verify OpenAPI spec matches implementation |
| Epic sandbox unavailable | Cache warm responses for demo scenarios |
| LLM latency in integration | Use cached responses for dev testing |
