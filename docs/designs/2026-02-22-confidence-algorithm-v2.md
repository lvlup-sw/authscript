# LCD-Anchored Confidence Algorithm & Policy Engine v2

**Date:** 2026-02-22
**Feature ID:** `confidence-algorithm-v2`
**Status:** Draft

## Problem Statement

The Intelligence service's confidence scoring is a naive 3-tier fixed system:

| Condition | Score | Recommendation |
|-----------|-------|----------------|
| All required MET, none NOT_MET/UNCLEAR | **0.9** | APPROVE |
| Any UNCLEAR (no NOT_MET) | **0.7** | MANUAL_REVIEW |
| Any NOT_MET | **0.6** | NEED_INFO |

This produces identical scores for wildly different clinical scenarios — a request missing 1 of 4 criteria scores the same 60% as one missing all 4. Per-criterion LLM confidence values (0.8/0.5) are computed but never used in the final score. The single hardcoded `EXAMPLE_POLICY` references no real insurance policy source.

**Target:** A credible, auditable confidence score that traces to CMS Local Coverage Determinations (LCDs), varies continuously based on how many weighted criteria are satisfied and how confident the LLM is in each evaluation.

## Architecture Context

```
Intelligence Service (Python/FastAPI)
┌──────────────────────────────────────────────────────┐
│                                                      │
│  POST /analyze                                       │
│       │                                              │
│       ▼                                              │
│  ┌─────────────┐    ┌──────────────────────────────┐ │
│  │ Policy       │◄──│ PolicyRegistry               │ │
│  │ Resolution   │   │  ├─ LCD-backed policies (seed)│ │
│  │              │   │  └─ Generic fallback policy   │ │
│  └──────┬──────┘   └──────────────────────────────┘ │
│         │                                            │
│         ▼                                            │
│  ┌──────────────┐                                    │
│  │ Evidence      │  Per-criterion LLM evaluation     │
│  │ Extractor     │  (parallel, semaphore-bounded)    │
│  └──────┬───────┘                                    │
│         │                                            │
│         ▼                                            │
│  ┌──────────────┐                                    │
│  │ Confidence    │  NEW: Weighted LCD compliance      │
│  │ Scorer        │  scoring algorithm                │
│  └──────┬───────┘                                    │
│         │                                            │
│         ▼                                            │
│  ┌──────────────┐                                    │
│  │ Form          │  Clinical summary + response      │
│  │ Generator     │                                   │
│  └──────────────┘                                    │
└──────────────────────────────────────────────────────┘
```

### What's changing:
- `EXAMPLE_POLICY` dict → **PolicyRegistry** with LCD-backed Pydantic models
- Hardcoded 3-tier confidence → **Weighted criterion aggregation** formula
- Single policy → **5 seed policies** + generic fallback
- `analyze.py` hard-rejects unsupported CPTs → **graceful generic fallback**

### What's NOT changing:
- Evidence extractor LLM evaluation loop (works well)
- Form generator clinical summary generation
- LLM client / provider architecture
- API contract (PAFormResponse shape stays the same)
- Gateway IntelligenceClient (already wired correctly)

## Design

### Component 1: Policy Data Model

Replace the untyped `dict[str, Any]` policy structure with validated Pydantic models.

**File:** `apps/intelligence/src/models/policy.py` (new)

```python
from pydantic import BaseModel

class PolicyCriterion(BaseModel):
    id: str                           # e.g. "conservative_therapy"
    description: str                  # LCD-sourced requirement text
    weight: float                     # 0.0-1.0, clinical importance
    required: bool = False            # Hard gate — if NOT_MET, caps score
    lcd_section: str | None = None    # e.g. "L34220 §4.2 - Conservative Mgmt"

class PolicyDefinition(BaseModel):
    policy_id: str                    # e.g. "lcd-mri-lumbar-L34220"
    policy_name: str
    lcd_reference: str | None = None  # e.g. "L34220"
    lcd_title: str | None = None      # e.g. "Lumbar MRI"
    lcd_contractor: str | None = None # e.g. "Noridian Healthcare Solutions"
    payer: str                        # e.g. "CMS Medicare"
    procedure_codes: list[str]
    diagnosis_codes: list[str]        # Covered ICD-10 codes (subset)
    criteria: list[PolicyCriterion]
```

### Component 2: Policy Registry

A registry that resolves a procedure code to a policy definition. Seed policies are loaded at startup; unsupported procedure codes fall back to a generic medical necessity policy.

**File:** `apps/intelligence/src/policies/registry.py` (new)

```python
class PolicyRegistry:
    """Resolves procedure codes to LCD-backed policy definitions."""

    def __init__(self) -> None:
        self._by_cpt: dict[str, PolicyDefinition] = {}

    def register(self, policy: PolicyDefinition) -> None:
        for cpt in policy.procedure_codes:
            self._by_cpt[cpt] = policy

    def resolve(self, procedure_code: str) -> PolicyDefinition:
        """Return LCD-backed policy if available, else generic fallback."""
        if procedure_code in self._by_cpt:
            return self._by_cpt[procedure_code]
        return build_generic_policy(procedure_code)

# Module-level singleton, populated at import time
registry = PolicyRegistry()
```

**File:** `apps/intelligence/src/policies/generic_policy.py` (new)

The generic fallback policy evaluates three universal criteria:
1. Medical necessity documented (weight: 0.40)
2. Valid diagnosis code present (weight: 0.30)
3. Conservative therapy attempted (weight: 0.30)

Returns `lcd_reference = None` — the response will show these as "General Medical Necessity" criteria rather than LCD-backed. Lower base confidence for generic evaluations.

### Component 3: Seed Policies (LCD-Backed)

Five LCD-backed seed policies, each referencing real CMS LCD articles with criteria extracted from the LCD text.

**File:** `apps/intelligence/src/policies/seed/` (new directory)

#### Policy 1: MRI Lumbar Spine — LCD L34220

| Criterion | Weight | Required | LCD Section |
|-----------|--------|----------|-------------|
| `diagnosis_present` — Valid ICD-10 for lumbar pathology | 0.15 | Yes | L34220 / A57206 — Covered Diagnoses |
| `red_flag_screening` — Cauda equina, tumor, infection, major neuro deficit | 0.25 | No | L34220 — Immediate MRI Indications |
| `conservative_therapy_4wk` — 4+ weeks conservative management (NSAIDs, PT, activity mod) documented | 0.30 | Yes* | L34220 — Non-Red-Flag Requirements |
| `clinical_rationale` — Imaging abnormalities alone insufficient; supporting clinical rationale documented | 0.20 | Yes | L34220 — Coverage Principle |
| `no_duplicate_imaging` — No recent duplicative CT/MRI without new justification | 0.10 | No | L34220 — Non-Covered Indications |

*`conservative_therapy_4wk` is bypassed if `red_flag_screening` is MET (LCD allows immediate MRI for red flags).

CPT: 72148, 72149, 72158

#### Policy 2: MRI Brain — LCD L37373

| Criterion | Weight | Required | LCD Section |
|-----------|--------|----------|-------------|
| `diagnosis_present` — Valid ICD-10 for neurological condition | 0.15 | Yes | L37373 / A57204 — Covered Diagnoses |
| `neurological_indication` — Tumor, stroke, MS, seizures, unexplained neuro deficit | 0.35 | Yes | L37373 — Indications for MRI |
| `ct_insufficient` — CT already performed and insufficient, or MRI specifically indicated | 0.25 | No | L37373 — MRI vs CT Selection |
| `clinical_documentation` — Supporting clinical findings documented | 0.25 | Yes | L37373 — Coverage Requirements |

CPT: 70551, 70552, 70553

#### Policy 3: Total Knee Arthroplasty — LCD L36575

| Criterion | Weight | Required | LCD Section |
|-----------|--------|----------|-------------|
| `diagnosis_present` — Valid ICD-10 for knee joint disease | 0.10 | Yes | L36575 / A57685 — Covered Diagnoses |
| `advanced_joint_disease` — Imaging showing joint space narrowing, osteophytes, sclerosis, AVN | 0.25 | Yes | L36575 — Advanced Joint Disease |
| `functional_impairment` — Pain/disability interfering with ADLs, increased with weight bearing | 0.25 | Yes | L36575 — Functional Impairment |
| `failed_conservative_mgmt` — Documented trials of NSAIDs, PT, assistive devices, injections | 0.30 | Yes | L36575 — Failed Conservative Management |
| `no_contraindication` — No active joint infection, systemic bacteremia, skin infection at site | 0.10 | Yes | L36575 — Contraindications |

CPT: 27447

#### Policy 4: Physical Therapy — LCD L34049

| Criterion | Weight | Required | LCD Section |
|-----------|--------|----------|-------------|
| `improvement_potential` — Patient condition has improvement potential or actively improving | 0.30 | Yes | L34049 — Rehabilitative Therapy |
| `skilled_service_required` — Service requires professional judgment, cannot be self-administered | 0.25 | Yes | L34049 — Skilled Service Requirements |
| `individualized_plan` — Plan of care with goals, frequency, duration documented | 0.25 | Yes | L34049 — Documentation Requirements |
| `objective_progress` — Successive objective measurements demonstrate progress | 0.20 | No | L34049 — Progress Documentation |

CPT: 97161, 97162, 97163

#### Policy 5: Epidural Steroid Injection — LCD L39240

| Criterion | Weight | Required | LCD Section |
|-----------|--------|----------|-------------|
| `diagnosis_confirmed` — Radiculopathy/stenosis confirmed by history, exam, and imaging | 0.25 | Yes | L39240 — Requirement 1 |
| `severity_documented` — Pain severe enough to impact QoL/function, documented with standardized scale | 0.20 | Yes | L39240 — Requirement 2 |
| `conservative_care_4wk` — 4 weeks conservative care failed/intolerable (except acute herpes zoster) | 0.25 | Yes | L39240 — Requirement 3 |
| `frequency_within_limits` — ≤4 sessions per region per rolling 12 months | 0.15 | Yes | L39240 — Frequency Limits |
| `image_guidance_planned` — Fluoroscopy or CT guidance with contrast planned | 0.15 | Yes | L39240 — Procedural Requirements |

CPT: 62322, 62323

### Component 4: Confidence Scoring Algorithm

**File:** `apps/intelligence/src/reasoning/confidence_scorer.py` (new)

Replace the 3-tier fixed scoring with a weighted LCD compliance formula.

#### Algorithm

```
For each criterion i with evidence evaluation:
  status_score(i) = 1.0 if MET, 0.5 if UNCLEAR, 0.0 if NOT_MET
  weight(i)       = criterion.weight (from policy, sums to 1.0)
  llm_conf(i)     = evidence.confidence (from LLM evaluation, 0.0-1.0)

raw_score = Σ(weight(i) × status_score(i) × llm_conf(i)) / Σ(weight(i) × llm_conf(i))

# Hard gates: required criteria that are NOT_MET impose a ceiling
required_not_met = [e for e in evidence if e.required and e.status == "NOT_MET"]
if required_not_met:
    gate_penalty = 0.15 × len(required_not_met)
    raw_score = min(raw_score, 0.65 - gate_penalty)  # Caps at 65% minus 15% per required miss

# Floor: never below 5% (to avoid showing 0% which implies system failure)
final_score = max(0.05, min(1.0, raw_score))
```

#### Recommendation Derivation

Recommendation is derived FROM the score, not the other way around:

```python
if final_score >= 0.80:
    recommendation = "APPROVE"
elif final_score >= 0.50:
    recommendation = "MANUAL_REVIEW"
else:
    recommendation = "NEED_INFO"
```

#### Score Characteristics

| Scenario | Expected Score |
|----------|---------------|
| All 5 criteria MET (high LLM conf) | ~88-92% → APPROVE |
| 4 of 5 MET, 1 optional UNCLEAR | ~78-82% → APPROVE |
| 4 of 5 MET, 1 required NOT_MET | ~50% (capped) → MANUAL_REVIEW |
| 3 of 5 MET, 2 required NOT_MET | ~35% (capped) → NEED_INFO |
| All UNCLEAR | ~50% → MANUAL_REVIEW |
| All NOT_MET | ~5% (floor) → NEED_INFO |
| 3 MET + red_flag bypasses conservative_therapy | ~85% → APPROVE |

#### Bypass Logic

Some criteria can bypass others (e.g., red flag symptoms bypass conservative therapy wait). The scorer checks for bypass relationships defined in the policy:

```python
class PolicyCriterion(BaseModel):
    # ... existing fields ...
    bypasses: list[str] = []  # criterion IDs this one bypasses when MET
```

If a criterion with `bypasses` is MET, the bypassed criteria are treated as MET for scoring purposes regardless of their actual evaluation.

### Component 5: Evidence Extractor Enhancement

**File:** `apps/intelligence/src/reasoning/evidence_extractor.py` (modified)

#### Change 1: Accept PolicyDefinition instead of dict

```python
async def extract_evidence(
    clinical_bundle: ClinicalBundle,
    policy: PolicyDefinition,       # was: dict[str, Any]
) -> list[EvidenceItem]:
```

#### Change 2: Include LCD context in LLM prompt

Enhance `evaluate_criterion` to include the LCD section reference in the prompt, giving the LLM authoritative context:

```python
user_prompt = f"""
Criterion: {criterion.description}
Policy Reference: {criterion.lcd_section or "General medical necessity"}

Clinical Data:
{clinical_summary}

Evaluate if this criterion is MET, NOT_MET, or UNCLEAR based on the clinical data.
Provide a brief explanation of the evidence found.
"""
```

#### Change 3: Numeric confidence from LLM

Ask the LLM to provide a confidence level (HIGH/MEDIUM/LOW) and map it:

```python
# Parse confidence signal from LLM response
if "HIGH CONFIDENCE" in response_upper:
    confidence = 0.9
elif "LOW CONFIDENCE" in response_upper:
    confidence = 0.5
else:
    confidence = 0.7  # default MEDIUM

# Existing status parsing unchanged
```

### Component 6: Form Generator Refactor

**File:** `apps/intelligence/src/reasoning/form_generator.py` (modified)

#### Change 1: Delegate scoring to ConfidenceScorer

Remove the inline 3-tier logic (lines 49-73) and replace with:

```python
from src.reasoning.confidence_scorer import calculate_confidence

score_result = calculate_confidence(evidence, policy)
recommendation = score_result.recommendation
confidence_score = score_result.score
```

#### Change 2: Include LCD reference in response

Add policy metadata to the response so the Gateway can display it:

```python
return PAFormResponse(
    # ... existing fields ...
    confidence_score=score_result.score,
    recommendation=score_result.recommendation,
    # Policy metadata (new fields, optional for backward compat)
    policy_id=policy.policy_id,
    lcd_reference=policy.lcd_reference,
)
```

### Component 7: Analyze Endpoint Update

**File:** `apps/intelligence/src/api/analyze.py` (modified)

Replace the hardcoded policy loading and procedure code rejection with registry lookup:

```python
from src.policies.registry import registry

@router.post("/analyze")
async def analyze(request: AnalyzeRequest) -> PAFormResponse:
    # No more hard 400 rejection for unsupported CPTs
    policy = registry.resolve(request.procedure_code)

    bundle = ClinicalBundle.from_dict(request.patient_id, request.clinical_data)
    evidence = await extract_evidence(bundle, policy)
    return await generate_form_data(bundle, evidence, policy)
```

### Component 8: PAFormResponse Model Update

**File:** `apps/intelligence/src/models/pa_form.py` (modified)

Add optional policy metadata fields:

```python
class PAFormResponse(BaseModel):
    # ... existing fields unchanged ...
    policy_id: str | None = None
    lcd_reference: str | None = None
```

These are optional with defaults so the Gateway's existing deserialization continues to work without changes.

## Data Flow (Target State)

```
POST /analyze { procedure_code: "72148", clinical_data: {...} }
     │
     ▼
PolicyRegistry.resolve("72148")
     │ → PolicyDefinition(lcd_reference="L34220", criteria=[...5 weighted...])
     │
     ▼
EvidenceExtractor.extract_evidence(bundle, policy)
     │ → 5 parallel LLM calls, each with LCD section context
     │ → [EvidenceItem(status=MET, conf=0.9), EvidenceItem(status=NOT_MET, conf=0.8), ...]
     │
     ▼
ConfidenceScorer.calculate_confidence(evidence, policy)
     │ → raw_score = Σ(weight × status × conf) / Σ(weight × conf)
     │ → apply hard gates for required NOT_MET criteria
     │ → ScoreResult(score=0.62, recommendation="MANUAL_REVIEW")
     │
     ▼
FormGenerator.generate_form_data(bundle, evidence, policy)
     │ → LLM generates clinical summary
     │ → PAFormResponse(confidence_score=0.62, lcd_reference="L34220", ...)
     │
     ▼
Gateway receives response, maps to PARequestModel
     │ → confidence = 62%, criteria with human-readable labels
     │
     ▼
Dashboard displays: "62% confidence — LCD L34220 compliance"
```

## Testing Strategy

### Layer 1: Confidence Scorer Unit Tests (pytest)

**File:** `apps/intelligence/src/tests/test_confidence_scorer.py` (new)

| Test | Description |
|------|-------------|
| `test_all_met_high_confidence` | All criteria MET with 0.9 conf → score ≥ 0.85 |
| `test_all_not_met` | All NOT_MET → score = 0.05 (floor) |
| `test_mixed_met_not_met` | 3 MET + 1 NOT_MET (optional) → score ~0.75 |
| `test_required_not_met_caps_score` | Required criterion NOT_MET → score ≤ 0.50 |
| `test_unclear_contributes_half` | UNCLEAR status contributes 0.5 × weight |
| `test_bypass_logic` | Red flag MET → conservative therapy treated as MET |
| `test_multiple_required_not_met_stacks_penalty` | 2 required NOT_MET → lower cap than 1 |
| `test_recommendation_thresholds` | Score→recommendation mapping at boundaries |
| `test_weights_sum_to_one` | Verify all seed policies have weights summing to ~1.0 |
| `test_generic_policy_lower_confidence` | Generic fallback produces conservative scores |

### Layer 2: Policy Registry Tests (pytest)

**File:** `apps/intelligence/src/tests/test_policy_registry.py` (new)

| Test | Description |
|------|-------------|
| `test_resolve_known_cpt` | 72148 → MRI Lumbar policy |
| `test_resolve_unknown_cpt_returns_generic` | 99999 → Generic policy |
| `test_all_seed_policies_registered` | 5 policies × their CPT codes all resolve |
| `test_policy_criteria_weights_valid` | All weights in 0.0-1.0, sum ≈ 1.0 |
| `test_lcd_references_populated` | Seed policies have non-null lcd_reference |

### Layer 3: Integration Tests (pytest)

**File:** `apps/intelligence/src/tests/test_analyze.py` (expanded)

| Test | Description |
|------|-------------|
| `test_analyze_mri_lumbar_uses_lcd_policy` | 72148 → response includes lcd_reference="L34220" |
| `test_analyze_unknown_cpt_no_400` | 99999 → 200 OK with generic policy |
| `test_analyze_confidence_varies_with_evidence` | Different clinical data → different scores |
| `test_analyze_response_backward_compatible` | Existing fields unchanged, new fields optional |

### Layer 4: Evidence Extractor Tests (expanded)

**File:** `apps/intelligence/src/tests/test_evidence_extractor.py` (expanded)

| Test | Description |
|------|-------------|
| `test_lcd_context_in_prompt` | LLM prompt includes LCD section reference |
| `test_accepts_policy_definition` | Works with PolicyDefinition (not just dict) |

### Layer 5: Gateway Tests (existing — verify no breakage)

No Gateway code changes needed. Run existing tests to confirm:
- `PAFormData` deserialization handles new optional fields gracefully
- Integration test bootstraps still pass with mock IntelligenceClient

## File Change Summary

### New Files

| File | Description |
|------|-------------|
| `intelligence/src/models/policy.py` | PolicyDefinition + PolicyCriterion Pydantic models |
| `intelligence/src/policies/registry.py` | PolicyRegistry singleton + resolve logic |
| `intelligence/src/policies/generic_policy.py` | Generic fallback policy builder |
| `intelligence/src/policies/seed/__init__.py` | Seed policy loader |
| `intelligence/src/policies/seed/mri_lumbar.py` | LCD L34220 — MRI Lumbar Spine |
| `intelligence/src/policies/seed/mri_brain.py` | LCD L37373 — MRI Brain |
| `intelligence/src/policies/seed/tka.py` | LCD L36575 — Total Knee Arthroplasty |
| `intelligence/src/policies/seed/physical_therapy.py` | LCD L34049 — Physical Therapy |
| `intelligence/src/policies/seed/epidural_steroid.py` | LCD L39240 — Epidural Steroid Injection |
| `intelligence/src/reasoning/confidence_scorer.py` | Weighted LCD compliance scoring algorithm |
| `intelligence/src/tests/test_confidence_scorer.py` | Scorer unit tests |
| `intelligence/src/tests/test_policy_registry.py` | Registry tests |

### Modified Files

| File | Change |
|------|--------|
| `intelligence/src/api/analyze.py` | Replace hardcoded policy + 400 rejection with registry.resolve() |
| `intelligence/src/reasoning/evidence_extractor.py` | Accept PolicyDefinition; LCD context in prompts; confidence parsing |
| `intelligence/src/reasoning/form_generator.py` | Delegate scoring to ConfidenceScorer; pass policy metadata |
| `intelligence/src/models/pa_form.py` | Add optional policy_id, lcd_reference fields |
| `intelligence/src/policies/example_policy.py` | Migrate to seed/mri_lumbar.py (delete or keep as reference) |
| `intelligence/src/tests/test_analyze.py` | Expand for new policy resolution |
| `intelligence/src/tests/test_evidence_extractor.py` | Expand for PolicyDefinition + LCD context |
| `intelligence/src/tests/test_form_generator.py` | Update for scorer delegation |

### Unchanged

| Component | Reason |
|-----------|--------|
| Gateway IntelligenceClient | PAFormResponse shape is backward compatible |
| Gateway Mutation.cs | Already maps criteria correctly; new fields are optional |
| Dashboard | Confidence % display works with any 0-100 value |
| LLM client | No changes to provider/completion logic |

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| LCD criteria extraction may be imprecise | Handcrafted seed policies ensure quality for top 5 procedures; LLM fallback handles the long tail |
| Weighted formula may produce unintuitive scores | Extensive unit tests at boundary conditions; tunable weights per policy |
| Generic fallback may be too generous/conservative | Default to conservative (lower base weights); flag generic results in response |
| LLM confidence parsing adds noise | Default to 0.7 if no confidence signal; bounded by weight system |
| New optional response fields may break Gateway | Fields are optional with None defaults; existing deserialization ignores unknown fields |
| Policy weights don't sum to 1.0 | Validation in PolicyDefinition model; test coverage |
