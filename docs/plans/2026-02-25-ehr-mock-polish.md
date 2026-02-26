# Implementation Plan: EHR Mock Polish

**Feature ID:** `ehr-mock-polish`
**Design:** `docs/designs/2026-02-25-ehr-mock-polish.md`
**Date:** 2026-02-25

## Task Overview

| Task | Description | Phase | Dependencies | Parallelizable |
|------|-------------|-------|--------------|----------------|
| 1 | Data consistency — unify patient identity | RED→GREEN→REFACTOR | None | Yes |
| 2 | EncounterSidebar component | RED→GREEN→REFACTOR | None | Yes |
| 3 | Encounter metadata bar | RED→GREEN→REFACTOR | None | Yes |
| 4 | Collapsible section cards + vitals + orders | RED→GREEN→REFACTOR | Task 1 (data) | Yes (after T1) |
| 5 | Two-column layout + sidebar integration | RED→GREEN→REFACTOR | Tasks 2, 4 | No |
| 6 | Iframe loading skeleton | RED→GREEN→REFACTOR | None | Yes |
| 7 | CSS polish — extract inline animation | RED→GREEN→REFACTOR | None | Yes |
| 8 | Integration smoke test | RED→GREEN | Tasks 1–7 | No |

---

### Task 1: Data Consistency — Unify Patient Identity

**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test: `DemoData_EhrPatient_MatchesIntelligenceFixture`
   - File: `apps/dashboard/src/lib/demoData.test.ts`
   - Assert `DEMO_EHR_PATIENT.name === "Rebecca Sandbox"`
   - Assert `DEMO_EHR_PATIENT.dob === "09/14/1990"`
   - Assert `DEMO_EHR_PATIENT.mrn === "ATH60182"`
   - Assert `DEMO_ENCOUNTER.hpi` contains `"35-year-old"` (not `"45-year-old"`)
   - Assert `DEMO_ENCOUNTER.cc` is consistent with fixture clinical_summary
   - Expected failure: Current values are "Maria Garcia", "03/15/1981", "MRN-60182", "45-year-old"

2. **[GREEN]** Update `demoData.ts`:
   - Change `DEMO_EHR_PATIENT` name to `"Rebecca Sandbox"`, dob to `"09/14/1990"`, mrn to `"ATH60182"`
   - Update `DEMO_ENCOUNTER` text: change age to 35, align clinical details with `demo_mri_lumbar.json`
   - Add `DEMO_VITALS` object: `{ bp: "128/82", hr: 72, temp: 98.6, spo2: 99 }`
   - Add `DEMO_ORDERS` array: `[{ code: "72148", name: "MRI Lumbar Spine w/o Contrast", status: "requires-pa" }]`
   - Add `DEMO_ENCOUNTER_META`: `{ provider: "Dr. Sarah Chen, MD", specialty: "Family Medicine", date: "02/25/2026", type: "Office Visit" }`

3. **[REFACTOR]** Extract shared types/interfaces for encounter data if needed.

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 2: EncounterSidebar Component

**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests: `EncounterSidebar_Renders_AllStages`, `EncounterSidebar_ActiveStage_Highlighted`, `EncounterSidebar_SignedState_UpdatesSignOff`
   - File: `apps/dashboard/src/components/ehr/EncounterSidebar.test.tsx`
   - Assert renders 6 stages: Review, HPI, ROS, PE, A&P, Sign-Off
   - Assert A&P has active styling (e.g., `aria-current="step"`)
   - Assert stages before A&P show completed state
   - Assert Sign-Off shows pending state initially, completed when `signed=true`
   - Expected failure: Component does not exist

2. **[GREEN]** Create `EncounterSidebar.tsx`:
   - Props: `{ activeStage: string; signed: boolean }`
   - Render vertical stage list with icons/indicators
   - Stages: `["Review", "HPI", "ROS", "PE", "A&P", "Sign-Off"]`
   - Active stage: bold + accent color + filled indicator
   - Completed stages (before active): muted + check indicator
   - Pending stages (after active): muted + empty indicator
   - Sign-Off: pending → completed when `signed` prop is true

3. **[REFACTOR]** Extract stage config to a constant. Add `aria-current="step"` for accessibility.

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 3: Encounter Metadata Bar

**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests: `EhrHeader_Metadata_RendersProviderAndDate`, `EhrHeader_Metadata_ShowsVisitType`
   - File: `apps/dashboard/src/components/ehr/EhrHeader.test.tsx`
   - Assert renders provider name "Dr. Sarah Chen, MD"
   - Assert renders specialty "Family Medicine"
   - Assert renders encounter date
   - Assert renders visit type "Office Visit"
   - Expected failure: `EhrHeader` does not accept or render metadata

2. **[GREEN]** Update `EhrHeader.tsx`:
   - Add optional `encounterMeta` prop: `{ provider: string; specialty: string; date: string; type: string }`
   - Render metadata row below patient banner when prop is provided
   - Style: smaller text, gray, pipe-separated

3. **[REFACTOR]** Clean up interface types. Ensure backward compatibility (metadata is optional).

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 4: Collapsible Section Cards + Vitals + Orders

**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - `EncounterNote_CcHpi_CollapsedByDefault` — CC/HPI card shows collapsed state
   - `EncounterNote_AssessmentPlan_ExpandedByDefault` — A&P card is expanded
   - `EncounterNote_Vitals_RendersValues` — Vitals row shows BP, HR, Temp, SpO2
   - `EncounterNote_Orders_ShowsPaBadge` — Orders card shows procedure with "Requires PA" badge
   - File: `apps/dashboard/src/components/ehr/EncounterNote.test.tsx`
   - Expected failure: Current component has no collapsible behavior, no vitals, no orders

2. **[GREEN]** Rewrite `EncounterNote.tsx`:
   - Accept new props: `vitals`, `orders` (from `DEMO_VITALS`, `DEMO_ORDERS`)
   - Replace flat sections with collapsible cards (use `details`/`summary` or state-based toggle)
   - CC/HPI section: collapsed by default (shows label only)
   - Assessment & Plan: expanded by default
   - New Orders card: render each order with code, name, and status badge
   - Vitals row: static display above the cards
   - Keep existing encounter data interface backward-compatible

3. **[REFACTOR]** Extract `CollapsibleCard` helper if pattern repeats. Extract `VitalsRow` and `OrdersCard` as sub-components.

**Dependencies:** Task 1 (needs `DEMO_VITALS`, `DEMO_ORDERS` data)
**Parallelizable:** Yes (after Task 1 is complete)

---

### Task 5: Two-Column Layout + Sidebar Integration

**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - `EhrDemoPage_Renders_Sidebar` — Sidebar is present in the DOM
   - `EhrDemoPage_Layout_TwoColumn` — Sidebar and content area are siblings in a flex container
   - `EhrDemoPage_Signed_SidebarUpdates` — After signing, sidebar's Sign-Off stage updates
   - File: `apps/dashboard/src/routes/ehr-demo.test.tsx`
   - Expected failure: No sidebar in current layout

2. **[GREEN]** Update `ehr-demo.tsx`:
   - Wrap main content area in a two-column flex layout
   - Left column: `EncounterSidebar` (fixed width ~200px)
   - Right column: existing encounter content (metadata + cards + sign button + iframe)
   - Pass `signed` state to sidebar
   - Pass `DEMO_ENCOUNTER_META` to `EhrHeader`
   - Pass `DEMO_VITALS`, `DEMO_ORDERS` to `EncounterNote`

3. **[REFACTOR]** Ensure sidebar doesn't scroll with content on long pages. Clean up spacing.

**Dependencies:** Tasks 2 (sidebar), 4 (rewritten encounter note)
**Parallelizable:** No

---

### Task 6: Iframe Loading Skeleton

**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - `EmbeddedAppFrame_Loading_ShowsSkeleton` — Shows skeleton before iframe loads
   - `EmbeddedAppFrame_Loaded_HidesSkeleton` — Skeleton disappears after iframe `onLoad`
   - File: `apps/dashboard/src/components/ehr/EmbeddedAppFrame.test.tsx`
   - Expected failure: No loading skeleton in current component

2. **[GREEN]** Update `EmbeddedAppFrame.tsx`:
   - Add internal `loading` state (default `true`)
   - Render skeleton overlay (pulsing gray rectangles) when loading
   - Set `loading = false` on iframe `onLoad` event
   - Skeleton: mimic dashboard layout (header bar + card rows)

3. **[REFACTOR]** Extract skeleton pattern if reusable.

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 7: CSS Polish — Extract Inline Animation

**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test: `EhrDemoPage_NoInlineStyle_Tag` — Assert no `<style>` elements in rendered output
   - File: `apps/dashboard/src/routes/ehr-demo.test.tsx` (add to Task 5 test file)
   - Expected failure: Current page has inline `<style>` block

2. **[GREEN]** Extract the `fadeSlideIn` keyframe:
   - Add to Tailwind CSS (e.g., `@keyframes` in `src/index.css` or Tailwind `theme.extend`)
   - Replace inline `style={{ animation: ... }}` with a Tailwind utility class
   - Remove the `<style>` JSX block

3. **[REFACTOR]** Verify animation still works visually.

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 8: Integration Smoke Test

**Phase:** RED → GREEN

1. **[RED]** Write test: `EhrDemoPage_FullFlow_SignShowsEmbeddedApp`
   - File: `apps/dashboard/src/routes/ehr-demo.test.tsx`
   - Render the full `EhrDemoPage`
   - Assert patient name "Rebecca Sandbox" appears
   - Assert sidebar is present with 6 stages
   - Assert encounter metadata shows provider info
   - Assert vitals row is visible
   - Assert orders card shows "72148"
   - Click "Sign Encounter" button
   - Assert sidebar Sign-Off stage updates
   - Assert embedded app frame becomes visible
   - Assert no inline `<style>` tags

2. **[GREEN]** All code from Tasks 1–7 should make this pass. Fix any integration gaps.

**Dependencies:** Tasks 1–7
**Parallelizable:** No

---

## Parallel Execution Strategy

```
Batch 1 (parallel):  Task 1, Task 2, Task 3, Task 6, Task 7
Batch 2 (parallel):  Task 4 (needs T1)
Batch 3 (sequential): Task 5 (needs T2, T4)
Batch 4 (sequential): Task 8 (needs all)
```

Tasks 1, 2, 3, 6, 7 are fully independent and can run in separate worktrees.
Task 4 depends on Task 1's data exports but can run once T1 merges.
Task 5 integrates the sidebar (T2) with the rewritten encounter note (T4).
Task 8 is the final integration check.

## Test Files Created

| File | Tests |
|------|-------|
| `apps/dashboard/src/lib/demoData.test.ts` | 4+ assertions on data consistency |
| `apps/dashboard/src/components/ehr/EncounterSidebar.test.tsx` | 3 tests |
| `apps/dashboard/src/components/ehr/EhrHeader.test.tsx` | 2 tests |
| `apps/dashboard/src/components/ehr/EncounterNote.test.tsx` | 4 tests |
| `apps/dashboard/src/components/ehr/EmbeddedAppFrame.test.tsx` | 2 tests |
| `apps/dashboard/src/routes/ehr-demo.test.tsx` | 4+ tests (Tasks 5, 7, 8) |
