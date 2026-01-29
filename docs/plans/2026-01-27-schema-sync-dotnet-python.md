# Implementation Plan: Schema Sync .NET ↔ Python

## Source Design
Link: `docs/designs/2026-01-26-schema-sync-dotnet-python.md`

## Summary
- Total tasks: 8
- Parallel groups: 2
- Estimated test count: 16

## Overview

This plan implements bidirectional OpenAPI-based schema synchronization between the Gateway (.NET) and Intelligence (Python) services. The goal is to auto-generate consumer types from producer OpenAPI specs, eliminating manual type duplication.

**Contract Ownership:**

| Contract | Producer | Consumer |
|----------|----------|----------|
| ClinicalBundle | Gateway (.NET) | Intelligence (Python) |
| PAFormResponse | Intelligence (Python) | Gateway (.NET) |

---

## Task Breakdown

### Task 001: Create shared/schemas directory structure

**Phase:** GREEN (infrastructure task)

**Steps:**
1. [GREEN] Create directory structure
   - Create `shared/schemas/` directory
   - Add `.gitkeep` to track empty directory
   - Add `README.md` documenting the schema sync process

**Verification:**
- [ ] Directory exists: `shared/schemas/`
- [ ] README explains the schema ownership model

**Dependencies:** None
**Parallelizable:** Yes (Group A)
**Branch:** `feature/001-schemas-directory`

---

### Task 002: Add datamodel-code-generator to Intelligence dev dependencies

**Phase:** GREEN (infrastructure task)

**Steps:**
1. [GREEN] Add dependency
   - File: `apps/intelligence/pyproject.toml`
   - Add `datamodel-code-generator>=0.26.0` to dev dependencies
   - Run `uv sync` to install

**Verification:**
- [ ] `uv run datamodel-codegen --help` succeeds
- [ ] pyproject.toml updated

**Dependencies:** None
**Parallelizable:** Yes (Group A)
**Branch:** `feature/002-datamodel-codegen-dep`

---

### Task 003: Install NSwag .NET tool for C# generation

**Phase:** GREEN (infrastructure task)

**Steps:**
1. [GREEN] Add tool manifest and install
   - Create `.config/dotnet-tools.json` if not exists
   - Add NSwag.ConsoleCore to manifest
   - Document installation in README

**Verification:**
- [ ] `dotnet nswag version` succeeds (after restore)
- [ ] Tool manifest includes NSwag

**Dependencies:** None
**Parallelizable:** Yes (Group A)
**Branch:** `feature/003-nswag-tool`

---

### Task 004: Extract Gateway OpenAPI spec for ClinicalBundle types

**Phase:** RED → GREEN

**TDD Steps:**
1. [RED] Write test: `GatewayOpenApiExtraction_ContainsClinicalBundleSchema_True`
   - File: `scripts/build/__tests__/sync-schemas.test.ts`
   - Test that extracted gateway.openapi.json contains ClinicalBundle schema
   - Expected failure: File doesn't exist yet

2. [GREEN] Update sync-schemas.sh to extract Gateway spec
   - File: `scripts/build/sync-schemas.sh`
   - Add step to run `dotnet swagger tofile` and output to `shared/schemas/gateway.openapi.json`
   - Filter to include only model schemas (not endpoints)

3. [REFACTOR] Extract reusable functions in script

**Verification:**
- [ ] `shared/schemas/gateway.openapi.json` generated
- [ ] Contains ClinicalBundle, PatientInfo, ConditionInfo, etc.
- [ ] Test passes

**Dependencies:** 001
**Parallelizable:** Yes (Group A)
**Branch:** `feature/004-gateway-openapi-extract`

---

### Task 005: Extract Intelligence OpenAPI spec for PAFormResponse types

**Phase:** RED → GREEN

**TDD Steps:**
1. [RED] Write test: `IntelligenceOpenApiExtraction_ContainsPAFormResponseSchema_True`
   - File: `scripts/build/__tests__/sync-schemas.test.ts`
   - Test that extracted intelligence.openapi.json contains PAFormResponse schema
   - Expected failure: Spec missing or incomplete

2. [GREEN] Update sync-schemas.sh to extract Intelligence spec
   - File: `scripts/build/sync-schemas.sh`
   - Use `uv run python -c` to extract app.openapi() to `shared/schemas/intelligence.openapi.json`

3. [REFACTOR] Consolidate extraction logic

**Verification:**
- [ ] `shared/schemas/intelligence.openapi.json` generated
- [ ] Contains PAFormResponse, EvidenceItem schemas
- [ ] Test passes

**Dependencies:** 001
**Parallelizable:** Yes (Group B)
**Branch:** `feature/005-intelligence-openapi-extract`

---

### Task 006: Generate Python Pydantic models from Gateway OpenAPI

**Phase:** RED → GREEN

**TDD Steps:**
1. [RED] Write test: `GeneratedGatewayTypes_ImportSucceeds_True`
   - File: `apps/intelligence/tests/models/test_generated_types.py`
   - Test that `from src.models.generated.gateway_types import ClinicalBundle` works
   - Expected failure: Module doesn't exist

2. [RED] Write test: `GeneratedClinicalBundle_HasExpectedFields_True`
   - Same file
   - Test that ClinicalBundle has patient_id, patient, conditions, etc.
   - Expected failure: Class doesn't have fields

3. [GREEN] Add generation step to sync-schemas.sh
   - Run `datamodel-codegen` with:
     - `--input shared/schemas/gateway.openapi.json`
     - `--output apps/intelligence/src/models/generated/gateway_types.py`
     - `--output-model-type pydantic_v2.BaseModel`
     - `--use-standard-collections --use-union-operator`

4. [REFACTOR] Add header comment noting file is auto-generated

**Verification:**
- [ ] `apps/intelligence/src/models/generated/gateway_types.py` generated
- [ ] Contains ClinicalBundle, PatientInfo, ConditionInfo classes
- [ ] Import test passes
- [ ] Field validation test passes

**Dependencies:** 002, 004
**Parallelizable:** No (depends on 004)
**Branch:** `feature/006-generate-python-types`

---

### Task 007: Generate C# records from Intelligence OpenAPI

**Phase:** RED → GREEN

**TDD Steps:**
1. [RED] Write test: `GeneratedIntelligenceTypes_Compiles_True`
   - File: `apps/gateway/Gateway.API.Tests/Models/Generated/IntelligenceTypesTests.cs`
   - Test that IntelligenceTypes.cs compiles and PAFormResponse exists
   - Expected failure: File doesn't exist

2. [RED] Write test: `GeneratedPAFormResponse_HasExpectedProperties_True`
   - Same file
   - Test that PAFormResponse record has PatientName, ConfidenceScore, etc.
   - Expected failure: Properties missing

3. [GREEN] Add generation step to sync-schemas.sh
   - Run `dotnet nswag openapi2cscontroller` with:
     - `/input:shared/schemas/intelligence.openapi.json`
     - `/output:apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs`
     - `/generateRecordTypes:true`

4. [REFACTOR] Ensure namespace matches Gateway.API.Models.Generated

**Verification:**
- [ ] `apps/gateway/Gateway.API/Models/Generated/IntelligenceTypes.cs` generated
- [ ] Contains PAFormResponse, EvidenceItem records
- [ ] Compiles successfully
- [ ] Tests pass

**Dependencies:** 003, 005
**Parallelizable:** No (depends on 005)
**Branch:** `feature/007-generate-csharp-types`

---

### Task 008: Add CI drift detection step

**Phase:** RED → GREEN

**TDD Steps:**
1. [RED] Write test: `SchemaDriftDetection_CleanState_PassesCheck`
   - File: `scripts/build/__tests__/sync-schemas.test.ts`
   - Test that running sync-schemas twice produces identical output
   - Expected failure: Script doesn't have idempotency check

2. [GREEN] Add drift detection to sync-schemas.sh
   - After generation, run `git diff --exit-code` on:
     - `shared/schemas/`
     - `apps/intelligence/src/models/generated/`
     - `apps/gateway/Gateway.API/Models/Generated/`
   - Exit with error if changes detected (in CI mode)

3. [GREEN] Update CI workflow
   - File: `.github/workflows/ci.yml`
   - Add schema-sync job that:
     - Runs `npm run sync:schemas`
     - Fails if generated files differ from committed

**Verification:**
- [ ] `npm run sync:schemas` is idempotent
- [ ] CI fails when generated code drifts
- [ ] Test passes

**Dependencies:** 006, 007
**Parallelizable:** No (depends on all generation tasks)
**Branch:** `feature/008-ci-drift-detection`

---

## Parallelization Strategy

```text
                    ┌─────────────────────────────────────────────┐
                    │              Group A (Parallel)             │
                    ├─────────────────────────────────────────────┤
                    │                                             │
  ┌─────────┐       │  ┌─────────┐     ┌─────────┐               │
  │  001    │───────┼──│  004    │────►│  006    │───┐           │
  │ schemas │       │  │ gateway │     │ python  │   │           │
  │  dir    │       │  │ extract │     │  gen    │   │           │
  └─────────┘       │  └─────────┘     └─────────┘   │           │
                    │                                 │           │
                    └─────────────────────────────────┼───────────┘
                                                      │
  ┌─────────┐       ┌─────────────────────────────────┼───────────┐
  │  002    │       │              Group B (Parallel) │           │
  │datamodel│       ├─────────────────────────────────┼───────────┤
  │  dep    │       │                                 │           │
  └─────────┘       │  ┌─────────┐     ┌─────────┐   │           │
                    │  │  005    │────►│  007    │───┼──►┌───────┐│
  ┌─────────┐       │  │  intel  │     │  csharp │   │   │  008  ││
  │  003    │───────┼──│ extract │     │   gen   │───┼──►│  CI   ││
  │ nswag   │       │  └─────────┘     └─────────┘   │   │ drift ││
  │  tool   │       │                                 │   └───────┘│
  └─────────┘       │                                 │           │
                    └─────────────────────────────────┴───────────┘
```

### Execution Order

**Wave 1 (Parallel):**
- 001: Create schemas directory
- 002: Add datamodel-code-generator
- 003: Install NSwag tool

**Wave 2 (Parallel after Wave 1):**
- 004: Extract Gateway OpenAPI (needs 001)
- 005: Extract Intelligence OpenAPI (needs 001)

**Wave 3 (Parallel after Wave 2):**
- 006: Generate Python types (needs 002, 004)
- 007: Generate C# types (needs 003, 005)

**Wave 4 (Sequential):**
- 008: CI drift detection (needs 006, 007)

---

## Completion Checklist

- [ ] All tests written before implementation
- [ ] All tests pass
- [ ] `npm run sync:schemas` generates all types
- [ ] Generated Python types import cleanly
- [ ] Generated C# types compile cleanly
- [ ] CI drift detection works
- [ ] Ready for review
