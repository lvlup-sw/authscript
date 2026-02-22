# Implementation Plan: LCD-Anchored Confidence Algorithm v2

**Design:** `docs/designs/2026-02-22-confidence-algorithm-v2.md`
**Feature ID:** `confidence-algorithm-v2`

## Task Dependency Graph

```
T001 ──┐
       ├──► T004 ──► T006 ──► T008 ──► T009 ──► T010
T002 ──┤                 ▲
       ├──► T005 ────────┘
T003 ──┘
       ▲
T007 ──┘ (parallel with T004-T006)
```

**Parallel Groups:**
- **Group A** (foundation): T001, T002, T003 — no dependencies, fully parallel
- **Group B** (registry + integration): T004, T005, T007 — depend on Group A, parallel with each other
- **Group C** (wiring): T006, T008 — sequential, depend on Group B
- **Group D** (endpoint + compat): T009, T010 — sequential, depend on Group C

---

### Task 001: Policy Data Model
**Phase:** RED → GREEN → REFACTOR

1. [RED] Write tests for PolicyCriterion and PolicyDefinition models
   - File: `apps/intelligence/src/tests/test_policy_model.py`
   - `test_policy_criterion_valid` — construct with all fields, verify attributes
   - `test_policy_criterion_defaults` — required=False, lcd_section=None, bypasses=[]
   - `test_policy_definition_valid` — construct with criteria list, verify
   - `test_policy_definition_weight_validation` — weights outside 0-1 should fail (if validator added)
   - `test_policy_criterion_bypasses_field` — verify bypasses list works
   - Expected failure: `ModuleNotFoundError: No module named 'src.models.policy'`

2. [GREEN] Implement PolicyCriterion and PolicyDefinition Pydantic models
   - File: `apps/intelligence/src/models/policy.py` (new)
   - PolicyCriterion: id, description, weight, required, lcd_section, bypasses
   - PolicyDefinition: policy_id, policy_name, lcd_reference, lcd_title, lcd_contractor, payer, procedure_codes, diagnosis_codes, criteria

3. [REFACTOR] None expected

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 002: Confidence Scorer Algorithm
**Phase:** RED → GREEN → REFACTOR

1. [RED] Write comprehensive scorer tests
   - File: `apps/intelligence/src/tests/test_confidence_scorer.py`
   - `test_all_met_high_confidence` — 5 criteria all MET (conf=0.9) → score ≥ 0.85, recommendation=APPROVE
   - `test_all_not_met_hits_floor` — all NOT_MET → score = 0.05
   - `test_mixed_met_and_optional_not_met` — 3 MET + 1 NOT_MET (optional, weight=0.10) → score ~0.75
   - `test_required_not_met_caps_score` — 4 MET + 1 required NOT_MET → score ≤ 0.50
   - `test_multiple_required_not_met_stacks_penalty` — 2 required NOT_MET → score < single required NOT_MET
   - `test_unclear_contributes_half` — all UNCLEAR → score ~0.50
   - `test_bypass_treats_bypassed_as_met` — criterion with bypasses=[X] MET → X treated as MET
   - `test_bypass_ignored_when_bypasser_not_met` — bypass criterion NOT_MET → no effect
   - `test_recommendation_approve_threshold` — score=0.80 → APPROVE
   - `test_recommendation_manual_review_threshold` — score=0.50 → MANUAL_REVIEW
   - `test_recommendation_need_info_threshold` — score=0.49 → NEED_INFO
   - `test_score_floor_never_below_five_percent` — extreme inputs → min 0.05
   - `test_score_ceiling_never_above_one` — all perfect → max 1.0
   - Expected failure: `ModuleNotFoundError: No module named 'src.reasoning.confidence_scorer'`

2. [GREEN] Implement calculate_confidence function
   - File: `apps/intelligence/src/reasoning/confidence_scorer.py` (new)
   - ScoreResult dataclass with score + recommendation
   - Weighted formula: `Σ(weight × status_score × llm_conf) / Σ(weight × llm_conf)`
   - Hard gate logic for required NOT_MET criteria
   - Bypass logic
   - Floor (0.05) and ceiling (1.0) clamping
   - Recommendation derivation from score thresholds

3. [REFACTOR] None expected

**Dependencies:** Task 001 (uses PolicyCriterion for bypass logic)
**Parallelizable:** Yes (with T001 — can stub PolicyCriterion initially)

---

### Task 003: Generic Fallback Policy
**Phase:** RED → GREEN → REFACTOR

1. [RED] Write tests for generic policy builder
   - File: `apps/intelligence/src/tests/test_generic_policy.py`
   - `test_build_generic_policy_returns_policy_definition` — returns PolicyDefinition instance
   - `test_generic_policy_has_three_criteria` — medical_necessity, diagnosis_present, conservative_therapy
   - `test_generic_policy_weights_sum_to_one` — weights sum ≈ 1.0
   - `test_generic_policy_no_lcd_reference` — lcd_reference is None
   - `test_generic_policy_includes_procedure_code` — passed CPT code in procedure_codes
   - `test_generic_policy_payer_is_general` — payer field set to generic value
   - Expected failure: `ModuleNotFoundError: No module named 'src.policies.generic_policy'`

2. [GREEN] Implement build_generic_policy function
   - File: `apps/intelligence/src/policies/generic_policy.py` (new)
   - 3 universal criteria: medical_necessity (0.40), diagnosis_present (0.30), conservative_therapy (0.30)
   - All required=True except conservative_therapy (required=False — not applicable to all procedures)

3. [REFACTOR] None expected

**Dependencies:** Task 001 (uses PolicyDefinition/PolicyCriterion)
**Parallelizable:** Yes (with T001/T002)

---

### Task 004: Policy Registry + Seed Policy Loader
**Phase:** RED → GREEN → REFACTOR

1. [RED] Write registry tests
   - File: `apps/intelligence/src/tests/test_policy_registry.py`
   - `test_register_and_resolve_known_cpt` — register policy with CPT 72148, resolve → same policy
   - `test_resolve_unknown_cpt_returns_generic` — unregistered CPT → generic fallback
   - `test_register_multi_cpt_policy` — policy with 3 CPTs → all 3 resolve to it
   - `test_seed_policies_registered_on_import` — importing registry has pre-registered seeds
   - `test_all_seed_cpts_resolve_to_lcd_policy` — all 14 seed CPT codes resolve to LCD-backed policies
   - `test_seed_policy_lcd_references_populated` — all seed policies have non-null lcd_reference
   - `test_seed_policy_weights_sum_approximately_one` — all seed policies: Σ weights ≈ 1.0
   - Expected failure: `ModuleNotFoundError: No module named 'src.policies.registry'`

2. [GREEN] Implement PolicyRegistry + seed policies
   - File: `apps/intelligence/src/policies/registry.py` (new) — PolicyRegistry class + module-level singleton
   - File: `apps/intelligence/src/policies/seed/__init__.py` (new) — imports and registers all seed policies
   - File: `apps/intelligence/src/policies/seed/mri_lumbar.py` (new) — LCD L34220, CPTs 72148/72149/72158
   - File: `apps/intelligence/src/policies/seed/mri_brain.py` (new) — LCD L37373, CPTs 70551/70552/70553
   - File: `apps/intelligence/src/policies/seed/tka.py` (new) — LCD L36575, CPT 27447
   - File: `apps/intelligence/src/policies/seed/physical_therapy.py` (new) — LCD L34049, CPTs 97161/97162/97163
   - File: `apps/intelligence/src/policies/seed/epidural_steroid.py` (new) — LCD L39240, CPTs 62322/62323

3. [REFACTOR] None expected

**Dependencies:** Task 001, Task 003
**Parallelizable:** Yes (with T002, T005)

---

### Task 005: PAFormResponse Model Update
**Phase:** RED → GREEN → REFACTOR

1. [RED] Write tests for new optional fields
   - File: `apps/intelligence/src/tests/test_pa_form_model.py` (new)
   - `test_pa_form_response_backward_compat` — construct without new fields → defaults to None
   - `test_pa_form_response_with_policy_metadata` — construct with policy_id + lcd_reference → present
   - `test_pa_form_response_serialization_includes_new_fields` — .model_dump() includes policy_id, lcd_reference
   - `test_pa_form_response_serialization_omits_none` — with exclude_none, absent fields omitted
   - Expected failure: validation error (fields don't exist yet)

2. [GREEN] Add optional fields to PAFormResponse
   - File: `apps/intelligence/src/models/pa_form.py` (modified)
   - Add `policy_id: str | None = None` and `lcd_reference: str | None = None`

3. [REFACTOR] None expected

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 006: Evidence Extractor Enhancement
**Phase:** RED → GREEN → REFACTOR

1. [RED] Write tests for enhanced extractor
   - File: `apps/intelligence/src/tests/test_evidence_extractor.py` (expanded)
   - `test_extract_evidence_accepts_policy_definition` — pass PolicyDefinition instead of dict → works
   - `test_evaluate_criterion_includes_lcd_section_in_prompt` — mock LLM captures prompt, verify LCD section text present
   - `test_evaluate_criterion_confidence_parsing_high` — LLM response contains "HIGH CONFIDENCE" → conf=0.9
   - `test_evaluate_criterion_confidence_parsing_low` — LLM response contains "LOW CONFIDENCE" → conf=0.5
   - `test_evaluate_criterion_confidence_parsing_default` — no confidence signal → conf=0.7
   - `test_extract_evidence_parallel_with_policy_definition` — parallel execution still works with PolicyDefinition
   - Expected failure: TypeError (PolicyDefinition not accepted)

2. [GREEN] Modify evidence_extractor.py
   - Accept `PolicyDefinition | dict` (union type for backward compat during migration)
   - `evaluate_criterion` accepts `PolicyCriterion | dict` and includes lcd_section in prompt
   - Parse confidence signal (HIGH/MEDIUM/LOW) from LLM response
   - Update prompt to request confidence level

3. [REFACTOR] Remove dict fallback path once all callers use PolicyDefinition

**Dependencies:** Task 001, Task 004 (for PolicyDefinition type)
**Parallelizable:** No (modifies existing file used by other tasks)

---

### Task 007: Seed Policy Data Authoring
**Phase:** RED → GREEN → REFACTOR

1. [RED] Write validation tests for each seed policy
   - File: `apps/intelligence/src/tests/test_seed_policies.py` (new)
   - `test_mri_lumbar_lcd_reference` — policy.lcd_reference == "L34220"
   - `test_mri_lumbar_has_five_criteria` — len(criteria) == 5
   - `test_mri_lumbar_conservative_therapy_bypass` — red_flag_screening bypasses conservative_therapy_4wk
   - `test_mri_brain_lcd_reference` — policy.lcd_reference == "L37373"
   - `test_mri_brain_has_four_criteria` — len(criteria) == 4
   - `test_tka_lcd_reference` — policy.lcd_reference == "L36575"
   - `test_tka_has_five_criteria` — len(criteria) == 5
   - `test_physical_therapy_lcd_reference` — policy.lcd_reference == "L34049"
   - `test_epidural_lcd_reference` — policy.lcd_reference == "L39240"
   - `test_all_seed_weights_valid` — parametrized: all policies have weights in 0-1, sum ≈ 1.0
   - `test_all_seed_criteria_have_lcd_sections` — all criteria have non-null lcd_section
   - Expected failure: ImportError (seed modules don't exist yet — shared with T004)

2. [GREEN] Author the 5 seed policy definitions with full LCD-sourced criteria
   - Each policy file defines a module-level `POLICY` constant of type `PolicyDefinition`
   - Criteria descriptions sourced from LCD article text
   - Weights assigned per design document tables
   - bypass relationships defined (MRI lumbar: red_flag → conservative_therapy)

3. [REFACTOR] None expected

**Dependencies:** Task 001
**Parallelizable:** Yes (with T004 — T004 creates the files, T007 validates content; can be combined)

**Note:** T004 and T007 operate on the same files. In practice, they should be implemented together — T004 creates the registry skeleton + seed file stubs, T007 fills in the detailed policy content.

---

### Task 008: Form Generator Refactor
**Phase:** RED → GREEN → REFACTOR

1. [RED] Update existing form generator tests + add new ones
   - File: `apps/intelligence/src/tests/test_form_generator.py` (modified)
   - Update `sample_policy` fixture to return PolicyDefinition instead of dict
   - `test_generate_form_data_delegates_to_scorer` — mock confidence_scorer, verify it's called
   - `test_generate_form_data_uses_scorer_recommendation` — scorer returns MANUAL_REVIEW → response has MANUAL_REVIEW
   - `test_generate_form_data_uses_scorer_confidence` — scorer returns 0.72 → response.confidence_score == 0.72
   - `test_generate_form_data_includes_policy_metadata` — response has policy_id + lcd_reference from policy
   - `test_generate_form_data_accepts_policy_definition` — PolicyDefinition input → no error
   - Update existing tests to pass with new signature (PolicyDefinition instead of dict)
   - Expected failure: TypeError (form_generator still uses dict), missing confidence_scorer import

2. [GREEN] Modify form_generator.py
   - Change `policy` parameter type to `PolicyDefinition`
   - Remove inline 3-tier scoring logic (lines 49-73)
   - Import and call `calculate_confidence(evidence, policy)`
   - Add policy_id + lcd_reference to PAFormResponse construction
   - Extract procedure_code from `policy.procedure_codes[0]`

3. [REFACTOR] Clean up unused imports (Literal if no longer needed)

**Dependencies:** Task 002 (confidence_scorer), Task 005 (PAFormResponse fields), Task 006 (evidence extractor uses same policy type)
**Parallelizable:** No (modifies form_generator.py which is imported by analyze.py)

---

### Task 009: Analyze Endpoint Update
**Phase:** RED → GREEN → REFACTOR

1. [RED] Update existing analyze tests + add new ones
   - File: `apps/intelligence/src/tests/test_analyze.py` (modified)
   - `test_analyze_mri_lumbar_uses_lcd_policy` — CPT 72148 → response.lcd_reference == "L34220"
   - `test_analyze_unknown_cpt_returns_200_with_generic` — CPT 99999 → 200 OK, lcd_reference is None
   - `test_analyze_confidence_not_fixed_tier` — mock varied LLM responses → confidence varies (not exactly 0.6/0.7/0.9)
   - Update `test_analyze_returns_approve` — may need to adjust expected confidence value
   - Update `test_analyze_rejects_unsupported_procedure` → **DELETE** or change to expect 200
   - Update `test_analyze_requires_patient_dob` — still valid
   - Update `test_analyze_builds_field_mappings` — still valid
   - Expected failure: HTTPException still raised for unsupported CPTs

2. [GREEN] Modify analyze.py
   - Remove `SUPPORTED_PROCEDURE_CODES` set
   - Remove `if request.procedure_code not in SUPPORTED_PROCEDURE_CODES` block
   - Import `registry` from `src.policies.registry`
   - Replace `policy = {**EXAMPLE_POLICY, ...}` with `policy = registry.resolve(request.procedure_code)`
   - Pass PolicyDefinition to `extract_evidence` and `generate_form_data`
   - Update `/with-documents` endpoint similarly

3. [REFACTOR] Remove unused `EXAMPLE_POLICY` import

**Dependencies:** Task 004 (registry), Task 006 (extractor accepts PolicyDefinition), Task 008 (generator accepts PolicyDefinition)
**Parallelizable:** No (final integration point)

---

### Task 010: Gateway Backward Compatibility Verification
**Phase:** RED → GREEN

1. [RED/GREEN] Run existing Gateway tests to verify no breakage
   - Run: `dotnet run --project Gateway.API.Tests` from `apps/gateway/`
   - Verify all 473+ tests pass (PAFormData deserialization handles new optional fields)
   - Run: `npx vitest run` from `apps/dashboard/`
   - Verify all 206+ dashboard tests pass

2. No code changes expected — this is a verification task

**Dependencies:** Task 009 (all Intelligence changes complete)
**Parallelizable:** No (final verification)

---

## Parallelization Strategy

```
                ┌── T001 (Policy Model) ─────────┐
                │                                 │
Worktree 1 ─────┤                                 ├──► T004+T007 (Registry + Seeds) ──┐
                │                                 │                                    │
                ├── T002 (Confidence Scorer) ─────┤                                    │
                │                                 │                                    ├──► T008 (Form Gen) ──► T009 (Endpoint) ──► T010 (Verify)
                ├── T003 (Generic Policy) ────────┘                                    │
                │                                                                      │
Worktree 2 ─────┴── T005 (PAFormResponse) ─── T006 (Evidence Extractor) ──────────────┘
```

**Group A (parallel worktrees):** T001 + T002 + T003 + T005
**Group B (parallel worktrees):** T004+T007 + T006
**Group C (sequential):** T008 → T009 → T010

## Summary

| Task | Description | Files | Depends On |
|------|-------------|-------|------------|
| T001 | Policy Data Model | `models/policy.py` | — |
| T002 | Confidence Scorer Algorithm | `reasoning/confidence_scorer.py` | T001 |
| T003 | Generic Fallback Policy | `policies/generic_policy.py` | T001 |
| T004 | Policy Registry + Seed Loader | `policies/registry.py`, `policies/seed/` | T001, T003 |
| T005 | PAFormResponse Model Update | `models/pa_form.py` | — |
| T006 | Evidence Extractor Enhancement | `reasoning/evidence_extractor.py` | T001 |
| T007 | Seed Policy Data Authoring | `policies/seed/*.py` | T001 |
| T008 | Form Generator Refactor | `reasoning/form_generator.py` | T002, T005, T006 |
| T009 | Analyze Endpoint Update | `api/analyze.py` | T004, T006, T008 |
| T010 | Gateway Backward Compat Verification | (no changes) | T009 |

**Total: 10 tasks, ~50 tests**
