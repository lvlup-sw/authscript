# Implementation Plan: Demo Polish Refactor

**Feature ID:** `refactor-demo-polish`
**Brief:** Workflow state `refactor-demo-polish`
**Date:** 2026-02-25

---

## Task 1: Structured Criteria Reason Dialog

**Phase:** RED → GREEN → REFACTOR

Enhance the `CriteriaReasonDialog` in `analysis.$transactionId.tsx` to render AI reasoning as structured, scannable content instead of a text blob.

1. **[RED]** Write test: `CriteriaReasonDialog_Render_ShowsStructuredReasoning`
   - File: `apps/dashboard/src/routes/__tests__/analysis-criteria-dialog.test.tsx`
   - Render `CriteriaReasonDialog` with a multi-sentence reason string
   - Assert: Each sentence renders as a separate list item (not a single `<p>`)
   - Assert: Status badge and criterion label are visible
   - Assert: "AI Analysis" heading is visible
   - Expected failure: reason is rendered as a single `<p>` element

2. **[RED]** Write test: `CriteriaReasonDialog_Met_HighlightsKeyEvidence`
   - File: `apps/dashboard/src/routes/__tests__/analysis-criteria-dialog.test.tsx`
   - Render with `met=true` and a reason containing clinical terms
   - Assert: Key finding/conclusion is visually distinct (e.g., bold, highlight, or in a callout)
   - Expected failure: no formatting applied to reason text

3. **[RED]** Write test: `CriteriaReasonDialog_NotMet_ShowsGapIndicator`
   - File: `apps/dashboard/src/routes/__tests__/analysis-criteria-dialog.test.tsx`
   - Render with `met=false` and a reason explaining what's missing
   - Assert: A "what's missing" or gap section is visually indicated
   - Expected failure: no distinction between met/not-met reasoning format

4. **[GREEN]** Implement structured reason rendering
   - File: `apps/dashboard/src/routes/analysis.$transactionId.tsx`
   - Replace the single `<p>` in `CriteriaReasonDialog` (line 229) with:
     - Parse reason into sentences (split on `. ` or period-space)
     - Render first sentence as a bold summary/conclusion
     - Render remaining sentences as bullet-pointed evidence items
     - For `met=false`/`met=null`: add a subtle "gap" callout with the key missing item
   - Keep the existing modal frame, header, footer unchanged

5. **[REFACTOR]** Extract `formatReasonText()` helper if parsing logic is >10 lines

**Dependencies:** None
**Parallelizable:** Yes

---

## Task 2: EHR Demo — Inline PA Workflow State Machine

**Phase:** RED → GREEN → REFACTOR

Replace the iframe-based flow with a state machine that drives the EHR demo page through: `idle → signing → processing → reviewing → submitting → complete`.

1. **[RED]** Write test: `useEhrDemoFlow_InitialState_IsIdle`
   - File: `apps/dashboard/src/components/ehr/__tests__/useEhrDemoFlow.test.ts`
   - Call the hook
   - Assert: `state === 'idle'`, `paRequest === null`, `error === null`
   - Expected failure: hook doesn't exist

2. **[RED]** Write test: `useEhrDemoFlow_Sign_TransitionsToProcessing`
   - File: `apps/dashboard/src/components/ehr/__tests__/useEhrDemoFlow.test.ts`
   - Call `sign()` action
   - Assert: state transitions through `signing` → `processing`
   - Assert: `createPARequest` mutation was called with DEMO_PATIENT + DEMO_SERVICE
   - Assert: `processPARequest` mutation was called with the created request ID
   - Expected failure: hook doesn't exist

3. **[RED]** Write test: `useEhrDemoFlow_ProcessComplete_TransitionsToReviewing`
   - File: `apps/dashboard/src/components/ehr/__tests__/useEhrDemoFlow.test.ts`
   - Mock `processPARequest` to return a PARequest with `status: 'ready'`, `confidence: 88`
   - Assert: state becomes `'reviewing'`, `paRequest` is populated with full PARequest data
   - Expected failure: hook doesn't exist

4. **[RED]** Write test: `useEhrDemoFlow_Submit_TransitionsToComplete`
   - File: `apps/dashboard/src/components/ehr/__tests__/useEhrDemoFlow.test.ts`
   - Start from `'reviewing'` state, call `submit()`
   - Assert: state becomes `'submitting'` then `'complete'`
   - Assert: `submitPARequest` mutation was called
   - Expected failure: hook doesn't exist

5. **[RED]** Write test: `useEhrDemoFlow_ProcessError_TransitionsToError`
   - File: `apps/dashboard/src/components/ehr/__tests__/useEhrDemoFlow.test.ts`
   - Mock `processPARequest` to reject
   - Assert: state becomes `'error'`, `error` is populated
   - Expected failure: hook doesn't exist

6. **[GREEN]** Implement `useEhrDemoFlow` hook
   - File: `apps/dashboard/src/components/ehr/useEhrDemoFlow.ts`
   - State machine: `idle → signing → processing → reviewing → submitting → complete | error`
   - Actions: `sign()`, `submit()`, `reset()`
   - Uses existing hooks: `useCreatePARequest`, `useProcessPARequest`, `useSubmitPARequest` from `graphqlService.ts`
   - On `sign()`: create PA request with `DEMO_PATIENT` + `DEMO_SERVICE`, then immediately process it
   - On `submit()`: submit the PA request
   - Exposes: `{ state, paRequest, error, sign, submit, reset }`

7. **[REFACTOR]** Extract state type and action types for readability

**Dependencies:** None
**Parallelizable:** Yes (with Task 1)

---

## Task 3: EHR Demo — Inline PA Results Panel

**Phase:** RED → GREEN → REFACTOR

New component that renders PA analysis results (confidence ring, criteria list, clinical summary, submit button) directly within the EHR chrome — replacing the iframe.

1. **[RED]** Write test: `PAResultsPanel_Processing_ShowsAnimation`
   - File: `apps/dashboard/src/components/ehr/__tests__/PAResultsPanel.test.tsx`
   - Render `<PAResultsPanel state="processing" paRequest={null} />`
   - Assert: Processing animation steps are visible ("Reading clinical notes...", "Analyzing medical necessity...")
   - Expected failure: component doesn't exist

2. **[RED]** Write test: `PAResultsPanel_Reviewing_ShowsConfidenceAndCriteria`
   - File: `apps/dashboard/src/components/ehr/__tests__/PAResultsPanel.test.tsx`
   - Render with `state="reviewing"` and a mock PARequest (88% confidence, 5 criteria all met)
   - Assert: Confidence ring shows "88%"
   - Assert: "5/5 met" badge visible
   - Assert: Each criterion label is rendered
   - Assert: "Submit to Payer" button is visible
   - Expected failure: component doesn't exist

3. **[RED]** Write test: `PAResultsPanel_Reviewing_CriterionClick_ShowsReasonDialog`
   - File: `apps/dashboard/src/components/ehr/__tests__/PAResultsPanel.test.tsx`
   - Render with reviewing state, click a criterion
   - Assert: CriteriaReasonDialog opens with the criterion's reason
   - Expected failure: component doesn't exist

4. **[RED]** Write test: `PAResultsPanel_Complete_ShowsSuccessState`
   - File: `apps/dashboard/src/components/ehr/__tests__/PAResultsPanel.test.tsx`
   - Render with `state="complete"` and a submitted PARequest
   - Assert: Success message visible ("PA Submitted")
   - Assert: Submit button is gone or disabled
   - Expected failure: component doesn't exist

5. **[GREEN]** Implement `PAResultsPanel`
   - File: `apps/dashboard/src/components/ehr/PAResultsPanel.tsx`
   - Props: `state`, `paRequest`, `onSubmit`, `onCriterionClick`
   - Processing state: Reuse the 4-step processing animation pattern from `NewPAModal`
   - Reviewing state: Compact layout with ProgressRing (smaller, ~100px), criteria list using `CriteriaItem` pattern, clinical summary, "Submit to Payer" button
   - Complete state: Success checkmark with "PA Submitted to [payer]" message
   - Error state: Error message with retry option
   - Styled to fit within the EHR card layout (not a full-page takeover)

6. **[REFACTOR]** Extract `ProgressRing` from `analysis.$transactionId.tsx` into shared `components/ProgressRing.tsx` so both analysis page and PAResultsPanel can use it. Extract `CriteriaItem` and `CriteriaReasonDialog` similarly.

**Dependencies:** Task 1 (uses enhanced CriteriaReasonDialog), Task 2 (consumes state machine states)
**Parallelizable:** No (sequential after Tasks 1 and 2)

---

## Task 4: EHR Demo — Page Rewrite with Inline Flow

**Phase:** RED → GREEN → REFACTOR

Rewrite `ehr-demo.tsx` to use the inline PA workflow instead of the iframe.

1. **[RED]** Write test: `EhrDemoPage_Render_ShowsAllSections` (update existing)
   - File: `apps/dashboard/src/routes/__tests__/ehr-demo.test.tsx`
   - Render `<EhrDemoPage />`
   - Assert: athenaOne header, patient name, encounter note, vitals, orders visible
   - Assert: Sign Encounter button visible
   - Assert: **NO iframe in DOM** (remove old assertion that checked for iframe)
   - Assert: No PA results panel visible initially
   - Expected failure: may still pass if iframe is gone; tests the new baseline

2. **[RED]** Write test: `EhrDemoPage_Sign_ShowsProcessingThenResults`
   - File: `apps/dashboard/src/routes/__tests__/ehr-demo.test.tsx`
   - Mock `useCreatePARequest` and `useProcessPARequest` mutations
   - Click "Sign Encounter"
   - Assert: PAResultsPanel appears below encounter note
   - Assert: Processing animation visible initially
   - After mutation resolves: confidence ring and criteria appear
   - Expected failure: old page still uses iframe

3. **[RED]** Write test: `EhrDemoPage_Sign_OrderStatusUpdates`
   - File: `apps/dashboard/src/routes/__tests__/ehr-demo.test.tsx`
   - Mock mutations, click "Sign Encounter", wait for processing
   - Assert: Order status badge changes from "Requires PA" to a PA-in-progress or ready state
   - Expected failure: orders are static fixture data

4. **[RED]** Write test: `EhrDemoPage_Submit_ShowsSuccess`
   - File: `apps/dashboard/src/routes/__tests__/ehr-demo.test.tsx`
   - Go through full flow: sign → process → click "Submit to Payer"
   - Assert: Success state visible in PAResultsPanel
   - Assert: Order status updates to reflect submission
   - Expected failure: submit not wired up

5. **[GREEN]** Rewrite `ehr-demo.tsx`
   - File: `apps/dashboard/src/routes/ehr-demo.tsx`
   - Remove `EmbeddedAppFrame` import and usage
   - Add `useEhrDemoFlow()` hook for state management
   - Wire `SignEncounterButton.onSign` to `flow.sign()`
   - Render `PAResultsPanel` below encounter note when `flow.state !== 'idle'`
   - Pass dynamic order status derived from `flow.state` (idle → 'requires-pa', processing → 'pending', reviewing/complete → 'completed')
   - Wire sidebar: add a "Prior Auth" stage after "Sign-Off" that activates when PA is processing/reviewing

6. **[GREEN]** Update `EncounterNote` order status type
   - File: `apps/dashboard/src/components/ehr/EncounterNote.tsx`
   - Add `'pa-processing'` and `'pa-ready'` to the `Order.status` union if needed (or reuse existing statuses)
   - Add corresponding status badge colors

7. **[GREEN]** Update `EncounterSidebar` to support PA stage
   - File: `apps/dashboard/src/components/ehr/EncounterSidebar.tsx`
   - Add optional `paStage` prop: `'none' | 'processing' | 'reviewing' | 'submitted'`
   - When `paStage !== 'none'`, render a "Prior Auth" section below the encounter stages

8. **[REFACTOR]** Remove `EmbeddedAppFrame` component and its test file (dead code after iframe removal). Update `components/ehr/index.ts` exports. Clean up `demoData.ts` if any unused exports remain.

**Dependencies:** Tasks 2, 3
**Parallelizable:** No (assembly task)

---

## Task 5: Test Updates and Validation

**Phase:** RED → GREEN → REFACTOR

Update existing tests that assert iframe behavior, ensure no regressions.

1. **[RED]** Run full dashboard test suite — identify any failures from removed iframe / changed exports
   - `npx vitest run` from `apps/dashboard/`
   - Catalog failures

2. **[GREEN]** Fix broken tests:
   - `EmbeddedAppFrame.test.tsx` — delete (component removed)
   - `ehr-demo.test.tsx` — already rewritten in Task 4; remove any residual iframe assertions
   - `demoData.test.ts` — update if exports changed
   - `EncounterNote.test.tsx` — add tests for new order status variants
   - `EncounterSidebar.test.tsx` — add tests for PA stage rendering

3. **[REFACTOR]** Clean up test utilities and mocks

**Dependencies:** Task 4
**Parallelizable:** No (validation task)

---

## Parallelization Map

```
Phase 1 (parallel):
  ┌─ Task 1 (criteria dialog formatting) ──────────┐
  │                                                  │  Parallel
  └─ Task 2 (useEhrDemoFlow hook) ─────────────────┘
                    │
                    ▼
Phase 2 (sequential):
            Task 3 (PAResultsPanel)  ← depends on Tasks 1, 2
                    │
                    ▼
Phase 3 (sequential):
            Task 4 (ehr-demo page rewrite)  ← depends on Task 3
                    │
                    ▼
Phase 4 (sequential):
            Task 5 (test updates + validation)  ← depends on Task 4
```

**Maximum parallelism:**
- Wave 1: Tasks 1, 2 (2 parallel agents — criteria UI, hook logic)
- Wave 2: Task 3 (1 agent — panel component)
- Wave 3: Task 4 (1 agent — page assembly)
- Wave 4: Task 5 (1 agent — validation)

---

## Branch Strategy

| Task | Branch |
|------|--------|
| Task 1 | `refactor/demo-polish/criteria-dialog` |
| Task 2 | `refactor/demo-polish/ehr-flow-hook` |
| Task 3 | `refactor/demo-polish/pa-results-panel` |
| Task 4 | `refactor/demo-polish/ehr-page-rewrite` |
| Task 5 | `refactor/demo-polish/test-validation` |

Base branch: `main`
