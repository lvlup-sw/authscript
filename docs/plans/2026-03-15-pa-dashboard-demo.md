# Implementation Plan: Scene-Based PA Demo with Cinematic Transitions

**Feature ID:** `pa-dashboard-demo`
**Design:** `docs/designs/2026-03-15-pa-dashboard-demo.md`
**Date:** 2026-03-15

## Dependency Graph

```
Group 1 (Foundation) ─── sequential
  task-001 → task-002 → task-003

Group 2 (Fleet)  ─┐
Group 3 (EHR)    ─┼── parallel (after Group 1)
Group 4 (Case)   ─┘

Group 5 (Transitions) ── sequential (after Groups 2-4)

Group 6 (Backend Seed) ── independent (parallel with all frontend)
```

```
task-001 ──► task-002 ──► task-003 ──┬──► task-004 ──► task-005 ──► task-006 ──► task-007 ──┐
                                     ├──► task-008 ──► task-009 ──► task-010 ──► task-011 ──┤
                                     └──► task-012 ──► task-013 ──► task-014 ──────────────┤
                                                                                           └──► task-015 ──► task-016
task-017 (backend, independent) ─────────────────────────────────────────────────────────────────────────────────────►
```

## Test Framework

- **Frontend:** Vitest + React Testing Library + jsdom
- **Run:** `npx vitest run` from `apps/dashboard/`
- **Backend (seed script):** TUnit, `dotnet run --project Gateway.API.Tests` from `apps/gateway/`

---

## Group 1: Foundation (Sequential)

### Task 001: Install dependencies and create DemoProvider context
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test: `DemoProvider_ProvidesDefaultSceneState`
   - File: `apps/dashboard/src/components/demo/__tests__/DemoProvider.test.tsx`
   - Tests:
     - `DemoProvider_DefaultScene_IsEncounter` — default scene is "encounter"
     - `DemoProvider_SetScene_UpdatesContext` — calling setScene changes active scene
     - `DemoProvider_SelectedCaseId_IsNullByDefault` — no case selected initially
     - `DemoProvider_SetSelectedCaseId_UpdatesContext` — setting case ID persists
   - Expected failure: Module `../DemoProvider` does not exist

2. **[GREEN]** Install dependencies and implement DemoProvider
   - Run: `cd apps/dashboard && npm install motion @xyflow/react`
   - File: `apps/dashboard/src/components/demo/DemoProvider.tsx`
   - Exports: `DemoProvider`, `useDemoContext`
   - Context shape: `{ scene: 'encounter' | 'fleet' | 'case', setScene, selectedCaseId, setSelectedCaseId, demoState }`

3. **[REFACTOR]** Extract types to `apps/dashboard/src/components/demo/types.ts`

**Dependencies:** None
**Parallelizable:** No (first task)

---

### Task 002: SceneNav pill navigation component
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test: `SceneNav_RendersScenePills`
   - File: `apps/dashboard/src/components/demo/__tests__/SceneNav.test.tsx`
   - Tests:
     - `SceneNav_RendersThreePills_EncounterFleetCase` — renders 3 navigation buttons
     - `SceneNav_ActiveScene_HasFilledStyle` — active pill has distinct styling
     - `SceneNav_ClickPill_CallsSetScene` — clicking a pill calls setScene with correct value
     - `SceneNav_DemoControls_RendersResetButton` — reset button renders and calls reset handler
   - Expected failure: Module `../SceneNav` does not exist

2. **[GREEN]** Implement SceneNav
   - File: `apps/dashboard/src/components/demo/SceneNav.tsx`
   - Pill-style nav bar with scene buttons
   - Consumes `useDemoContext` for active scene state
   - Demo Controls section: Reset Demo button

3. **[REFACTOR]** None expected

**Dependencies:** task-001
**Parallelizable:** No (depends on DemoProvider)

---

### Task 003: Route scaffolding and SceneTransition wrapper
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test: `SceneTransition_RendersChildren`
   - File: `apps/dashboard/src/components/demo/__tests__/SceneTransition.test.tsx`
   - Tests:
     - `SceneTransition_RendersActiveScene_WithChildren` — renders child content for active scene
     - `SceneTransition_SceneChange_AnimatesTransition` — changing scene key triggers AnimatePresence
   - Expected failure: Module `../SceneTransition` does not exist

2. **[GREEN]** Implement SceneTransition and route files
   - File: `apps/dashboard/src/components/demo/SceneTransition.tsx`
     - Wraps children in `AnimatePresence` + `motion.div`
     - Keys on active scene name for exit/enter animations
   - File: `apps/dashboard/src/routes/fleet.tsx` — placeholder with SceneNav + "Fleet" heading
   - File: `apps/dashboard/src/routes/case.$caseId.tsx` — placeholder with SceneNav + "Case Detail" heading

3. **[REFACTOR]** None expected

**Dependencies:** task-002
**Parallelizable:** No (depends on SceneNav)

---

## Group 2: Fleet Dashboard (Parallel-safe after Group 1)

### Task 004: Fleet seed data and KPICards component
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - File: `apps/dashboard/src/lib/__tests__/fleetSeedData.test.ts`
     - `fleetSeedData_Returns48Cases` — seed data generates 48 PA requests
     - `fleetSeedData_DistributesAcrossStatuses` — cases distributed across all 6 statuses
     - `fleetSeedData_UsesAllTestPatients` — all 7 test patients appear
     - `fleetSeedData_IncludesMultiplePayers` — Aetna, UHC, Cigna all present
     - `fleetSeedData_ConfidenceScoresInRange` — all confidence scores 60-98%
   - File: `apps/dashboard/src/components/fleet/__tests__/KPICards.test.tsx`
     - `KPICards_RendersSixCards` — renders 6 KPI cards with correct labels
     - `KPICards_DisplaysValues_FromStats` — each card shows correct count from stats prop
     - `KPICards_ClickCard_CallsOnFilter` — clicking a card calls onFilter with status key
     - `KPICards_ActiveFilter_HasHighlightedBorder` — active filter card has distinct border
   - Expected failure: Modules do not exist

2. **[GREEN]** Implement
   - File: `apps/dashboard/src/lib/fleetSeedData.ts`
     - Exports `generateFleetData(): PARequest[]` — creates 48 mock PA requests
     - Uses test patients from `patients.ts`
     - Distributes across statuses per design spec
   - File: `apps/dashboard/src/components/fleet/KPICards.tsx`
     - Props: `stats: PAStats, activeFilter: string | null, onFilter: (status: string) => void`
     - 6-card horizontal grid with animated counters
     - Count-up animation via `motion.span` with `useMotionValue` + `useTransform`

3. **[REFACTOR]** Extract counter animation to `useCountUp` hook

**Dependencies:** task-003
**Parallelizable:** Yes (parallel with Groups 3, 4)

---

### Task 005: FleetCard and FleetView components
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - File: `apps/dashboard/src/components/fleet/__tests__/FleetCard.test.tsx`
     - `FleetCard_RendersPatientInitials` — shows initials from patient name
     - `FleetCard_RendersStatusDot_WithCorrectColor` — status dot color matches status
     - `FleetCard_RendersProcedureCode` — shows CPT code
     - `FleetCard_RendersPayerBadge` — shows payer name
     - `FleetCard_RendersConfidence_WhenAnalyzed` — shows confidence % when status is post-analysis
     - `FleetCard_HighlightCase_ShowsGlow` — highlighted case has glow class
     - `FleetCard_Click_CallsOnSelect` — clicking calls onSelect with case ID
   - File: `apps/dashboard/src/components/fleet/__tests__/FleetView.test.tsx`
     - `FleetView_RendersAllCases_AsFleetCards` — renders a FleetCard for each PA request
     - `FleetView_FilterByStatus_ShowsOnlyMatching` — applying status filter hides non-matching
     - `FleetView_HighlightedCaseId_PassesToFleetCard` — passes highlight prop to matching card
   - Expected failure: Modules do not exist

2. **[GREEN]** Implement
   - File: `apps/dashboard/src/components/fleet/FleetCard.tsx`
     - Props: `request: PARequest, highlighted?: boolean, onSelect: (id: string) => void`
     - Compact card (~120x80px) with initials avatar, status dot, procedure, payer
     - Wrapped in `motion.div` with layout animation
   - File: `apps/dashboard/src/components/fleet/FleetView.tsx`
     - Props: `requests: PARequest[], filter: string | null, highlightedCaseId?: string, onSelectCase: (id: string) => void`
     - Responsive grid of FleetCards with filtering
     - `AnimatePresence` for enter/exit of filtered cards

3. **[REFACTOR]** Extract status-to-color mapping to shared utility

**Dependencies:** task-004
**Parallelizable:** No (depends on task-004 for data types)

---

### Task 006: CasePipeline component
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test:
   - File: `apps/dashboard/src/components/fleet/__tests__/CasePipeline.test.tsx`
     - `CasePipeline_RendersSixStages` — renders all 6 pipeline stages
     - `CasePipeline_ShowsCountPerStage` — each stage shows correct case count
     - `CasePipeline_ClickStage_CallsOnFilter` — clicking a stage calls onFilter with stage key
     - `CasePipeline_ActiveStage_HasHighlight` — active filter stage is visually highlighted
   - Expected failure: Module does not exist

2. **[GREEN]** Implement
   - File: `apps/dashboard/src/components/fleet/CasePipeline.tsx`
     - Props: `stageCounts: Record<string, number>, activeStage: string | null, onFilter: (stage: string) => void`
     - Horizontal pipeline: Order Signed → PA Detected → Processing → Ready → Submitted → Payer Response
     - SVG connecting lines with animated flowing dots (CSS keyframes)
     - Count badges per stage

3. **[REFACTOR]** None expected

**Dependencies:** task-004
**Parallelizable:** Yes (parallel with task-005)

---

### Task 007: Fleet route page assembly and GraphQL wiring
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test:
   - File: `apps/dashboard/src/routes/__tests__/fleet.test.tsx`
     - `FleetPage_RendersSceneNav` — page includes SceneNav component
     - `FleetPage_RendersPageTitle_CommandCenter` — shows "Prior Authorization Command Center"
     - `FleetPage_RendersKPICards` — KPI cards section renders
     - `FleetPage_RendersCasePipeline` — pipeline section renders
     - `FleetPage_RendersFleetView` — fleet grid section renders
     - `FleetPage_KPICardClick_FiltersFleetView` — clicking KPI card filters the fleet
   - Expected failure: Fleet route renders placeholder, not full page

2. **[GREEN]** Implement full fleet route
   - File: `apps/dashboard/src/routes/fleet.tsx`
     - Layout: SceneNav top → Header → KPI Cards → CasePipeline → FleetView + ActivityFeed
     - Wire to existing GraphQL queries (`useGetPARequests`, `useGetPAStats`, `useGetActivity`)
     - Filter state: managed locally, applied to FleetView and synced to KPICards/CasePipeline
     - Fallback to `fleetSeedData` when GraphQL is unavailable

3. **[REFACTOR]** Extract filter logic to `useFleetFilters` hook

**Dependencies:** task-005, task-006
**Parallelizable:** No (needs FleetView, KPICards, CasePipeline)

---

## Group 3: Enhanced EHR (Parallel-safe after Group 1)

### Task 008: Enhanced demo data and ChartTabPanel component
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - File: `apps/dashboard/src/lib/__tests__/demoData.test.ts`
     - `demoChartData_HasProblemList_WithICDCodes` — problem list contains M54.5, M54.41
     - `demoChartData_HasMedications_WithDosages` — medication list contains Ibuprofen, Cyclobenzaprine
     - `demoChartData_HasAllergies` — allergy data present (NKDA)
     - `demoChartData_HasEmptyImagingHistory` — imaging history is empty array
   - File: `apps/dashboard/src/components/ehr/__tests__/ChartTabPanel.test.tsx`
     - `ChartTabPanel_Problems_RendersICDCodes` — Problems tab shows ICD codes and descriptions
     - `ChartTabPanel_Meds_RendersMedicationList` — Meds tab shows medication names and dosages
     - `ChartTabPanel_Allergies_RendersNKDA` — Allergies tab shows "No Known Drug Allergies"
     - `ChartTabPanel_Imaging_RendersNoHistory` — Imaging tab shows "No prior lumbar imaging"
   - Expected failure: Exports do not exist

2. **[GREEN]** Implement
   - File: `apps/dashboard/src/lib/demoData.ts` — add `DEMO_CHART_DATA` export with:
     - `problems: Array<{ code, description, category }>`
     - `medications: Array<{ name, dosage, frequency, status }>`
     - `allergies: Array<{ substance, reaction, severity }> | 'NKDA'`
     - `imagingHistory: Array<{ date, type, result }>` (empty for demo)
     - `labResults: Array<{ name, value, date }>` (1-2 entries)
   - File: `apps/dashboard/src/components/ehr/ChartTabPanel.tsx`
     - Props: `activeTab: string, chartData: ChartData`
     - Renders content for the selected chart tab
     - Compact layout suitable for sidebar width

3. **[REFACTOR]** None expected

**Dependencies:** task-003
**Parallelizable:** Yes (parallel with Groups 2, 4)

---

### Task 009: Enhanced EncounterSidebar with chart tabs
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test:
   - File: `apps/dashboard/src/components/ehr/__tests__/EncounterSidebar.test.tsx`
     - `EncounterSidebar_RendersChartTabs_SixTabs` — renders Problems, Meds, Allergies, Vitals, Imaging, Labs
     - `EncounterSidebar_ClickTab_ShowsTabPanel` — clicking a chart tab shows its content panel
     - `EncounterSidebar_RendersEncounterStages_SevenStages` — renders Intake through Sign stages
     - `EncounterSidebar_PADetected_ShowsPAStages` — when PA detected, additional PA stages appear
     - `EncounterSidebar_ActiveStage_HasTealIndicator` — active stage has teal styling
   - Expected failure: Current EncounterSidebar doesn't have chart tabs

2. **[GREEN]** Rewrite EncounterSidebar
   - File: `apps/dashboard/src/components/ehr/EncounterSidebar.tsx`
   - Split into two sections:
     - Top: Chart tab buttons + ChartTabPanel (collapsible)
     - Bottom: Encounter stage tracker (vertical stepper)
   - Props: keep existing + add `onTabChange`, `chartData`

3. **[REFACTOR]** Extract stage tracker to `EncounterStageTracker` sub-component if needed

**Dependencies:** task-008
**Parallelizable:** No (depends on ChartTabPanel)

---

### Task 010: Enhanced useEhrDemoFlow state machine
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test:
   - File: `apps/dashboard/src/components/ehr/__tests__/useEhrDemoFlow.test.ts`
     - `useEhrDemoFlow_InitialState_IsChartBrowsing` — starts in chart-browsing state
     - `useEhrDemoFlow_AddOrder_TransitionsToOrderEntry` — calling addOrder moves to order-entry
     - `useEhrDemoFlow_OrderEntry_TransitionsToPADetected` — order-entry auto-transitions to pa-detected after delay
     - `useEhrDemoFlow_PADetected_TransitionsToDocumenting` — pa-detected moves to documenting on user action
     - `useEhrDemoFlow_Documenting_TransitionsToFlagged` — documenting moves to flagged (existing flow)
     - `useEhrDemoFlow_Complete_TransitionsToTransitionToFleet` — completion auto-triggers transition state
     - `useEhrDemoFlow_TransitionToFleet_CallsOnTransition` — transition state calls onTransition callback
   - Expected failure: New states not in current state machine

2. **[GREEN]** Enhance state machine
   - File: `apps/dashboard/src/components/ehr/useEhrDemoFlow.ts`
   - Add new states: `chart-browsing`, `order-entry`, `pa-detected`, `documenting`, `transition-to-fleet`
   - Add new actions: `addOrder()`, `detectPA()`, `startDocumenting()`
   - Add `onTransition?: () => void` callback prop for Scene 1→2 trigger
   - Preserve all existing states and transitions (backward compatible)

3. **[REFACTOR]** Extract state type union to types file

**Dependencies:** task-003 (needs existing hook to enhance)
**Parallelizable:** Yes (parallel with tasks 008-009, no UI dependency)

---

### Task 011: Authorization detection notification and EhrHeader enhancements
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - File: `apps/dashboard/src/components/ehr/__tests__/AuthDetectionBanner.test.tsx`
     - `AuthDetectionBanner_WhenVisible_ShowsPAMessage` — renders "PA Required — Aetna LCD L34220 applies to CPT 72148"
     - `AuthDetectionBanner_WhenHidden_RendersNothing` — returns null when not visible
     - `AuthDetectionBanner_RendersPayerName` — shows payer name from props
     - `AuthDetectionBanner_RendersPolicyReference` — shows LCD reference
   - File: `apps/dashboard/src/components/ehr/__tests__/EhrHeader.test.tsx` (enhance existing tests)
     - `EhrHeader_RendersEncounterTypePill` — shows "Office Visit" pill
     - `EhrHeader_RendersFacilityName` — shows "Family Care Associates"
   - Expected failure: AuthDetectionBanner module doesn't exist; EhrHeader missing new elements

2. **[GREEN]** Implement
   - File: `apps/dashboard/src/components/ehr/AuthDetectionBanner.tsx` (new)
     - Props: `visible: boolean, payer: string, policyId: string, cptCode: string`
     - Slide-in notification bar using `motion.div` with `AnimatePresence`
     - Amber/teal accent, "Authorization Determination Engine" label
   - File: `apps/dashboard/src/components/ehr/EhrHeader.tsx` (enhance)
     - Add encounter type pill and facility name

3. **[REFACTOR]** None expected

**Dependencies:** task-010 (auth detection triggers from state machine)
**Parallelizable:** No (depends on state machine changes)

---

## Group 4: Case Detail (Parallel-safe after Group 1)

### Task 012: React Flow custom nodes
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - File: `apps/dashboard/src/components/case/__tests__/CustomNodes.test.tsx`
     - `PatientNode_RendersPatientName_AndMRN` — shows patient name, DOB, MRN, insurance
     - `PatientNode_RendersAvatar_WithInitials` — shows initials avatar
     - `EvidenceNode_RendersEvidenceText_WithSource` — shows evidence text and source badge
     - `CriteriaNode_MetStatus_HasGreenBorder` — met criteria has green border/check
     - `CriteriaNode_NotMetStatus_HasRedBorder` — not met criteria has red border/X
     - `CriteriaNode_IndeterminateStatus_HasAmberBorder` — indeterminate has amber border/?
     - `DecisionNode_RendersPayerName_AndPolicy` — shows payer and LCD reference
     - `DecisionNode_RendersConfidenceScore` — shows confidence percentage
   - Expected failure: Modules do not exist

2. **[GREEN]** Implement custom nodes
   - File: `apps/dashboard/src/components/case/PatientNode.tsx`
     - Custom React Flow node with Handle components
     - Patient info layout: avatar, name, DOB, MRN, insurance badge
   - File: `apps/dashboard/src/components/case/EvidenceNode.tsx`
     - Evidence text with source tag (HPI, Assessment, etc.)
     - Blue/slate border styling
   - File: `apps/dashboard/src/components/case/CriteriaNode.tsx`
     - Criterion name + status icon (check/X/?)
     - Status-dependent border color
   - File: `apps/dashboard/src/components/case/DecisionNode.tsx`
     - Payer info, policy reference, confidence ring
     - Animated confidence ring via `motion.circle` SVG

3. **[REFACTOR]** Extract shared node card styling to `NodeCard` base component

**Dependencies:** task-003 (needs @xyflow/react installed)
**Parallelizable:** Yes (parallel with Groups 2, 3)

---

### Task 013: CaseGraph and AnimatedEdge
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - File: `apps/dashboard/src/components/case/__tests__/CaseGraph.test.tsx`
     - `CaseGraph_RendersPatientNode` — graph contains a patient-type node
     - `CaseGraph_RendersEvidenceNodes_ForEachEvidence` — renders evidence nodes matching criteria evidence
     - `CaseGraph_RendersCriteriaNodes_ForEachCriterion` — renders criteria nodes matching PA criteria
     - `CaseGraph_RendersDecisionNode` — graph contains a decision-type node
     - `CaseGraph_RendersEdges_EvidenceToCriteria` — evidence-to-criteria edges exist
     - `CaseGraph_RendersEdges_CriteriaToDecision` — criteria-to-decision edges exist
   - File: `apps/dashboard/src/components/case/__tests__/AnimatedEdge.test.tsx`
     - `AnimatedEdge_RendersBaseSVGPath` — renders an SVG path element
   - Expected failure: Modules do not exist

2. **[GREEN]** Implement
   - File: `apps/dashboard/src/components/case/AnimatedEdge.tsx`
     - Custom React Flow edge with SVG `<animateMotion>` dots
     - Props: standard EdgeProps + `animationDelay: number`
     - Animated dot flows along edge path
   - File: `apps/dashboard/src/components/case/CaseGraph.tsx`
     - Props: `paRequest: PARequest`
     - Builds nodes/edges from PA request data:
       - 1 PatientNode, N EvidenceNodes, N CriteriaNodes, 1 DecisionNode
       - Edges connecting evidence → criteria → decision
     - Positions nodes in a left-to-right flow layout
     - Wraps in `ReactFlowProvider` + `ReactFlow`

3. **[REFACTOR]** Extract node/edge builders to `buildCaseGraphData(paRequest)` utility

**Dependencies:** task-012
**Parallelizable:** No (depends on custom nodes)

---

### Task 014: CaseTimeline and case detail route assembly
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - File: `apps/dashboard/src/components/case/__tests__/CaseTimeline.test.tsx`
     - `CaseTimeline_RendersPastPhases_WithCheckmarks` — completed phases show checkmarks
     - `CaseTimeline_RendersCurrentPhase_WithPulsingDot` — active phase has active styling
     - `CaseTimeline_RendersFuturePhases_WithDashedLine` — pending phases have muted/dashed style
     - `CaseTimeline_ShowsTimestamps` — each phase shows its timestamp
     - `CaseTimeline_ShowsDuration_BetweenPhases` — shows elapsed time between phases
   - File: `apps/dashboard/src/routes/__tests__/case.test.tsx`
     - `CaseDetailPage_RendersSceneNav` — page includes SceneNav
     - `CaseDetailPage_RendersCaseGraph` — left panel renders CaseGraph
     - `CaseDetailPage_RendersCaseTimeline` — right panel renders CaseTimeline
     - `CaseDetailPage_RendersPatientSummary` — right panel renders patient info card
   - Expected failure: Modules do not exist

2. **[GREEN]** Implement
   - File: `apps/dashboard/src/components/case/CaseTimeline.tsx`
     - Props: `phases: Array<{ name, status, timestamp, duration? }>`
     - Vertical timeline with phase-appropriate styling
     - Past: solid line + gray + checkmark
     - Current: pulsing dot + bold + active color
     - Future: dashed line + muted
   - File: `apps/dashboard/src/routes/case.$caseId.tsx` — full implementation
     - Split layout: CaseGraph (55%) + detail cards (45%)
     - Detail cards: CaseTimeline, PatientSummary, CriteriaEvidence, ClinicalSummary
     - Loads PA request data from GraphQL or demo data fallback

3. **[REFACTOR]** None expected

**Dependencies:** task-013
**Parallelizable:** No (depends on CaseGraph)

---

## Group 5: Transitions & Integration (Sequential, after Groups 2-4)

### Task 015: Scene transitions with shared layout animations
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write tests:
   - File: `apps/dashboard/src/components/demo/__tests__/SceneTransition.test.tsx` (enhance from task-003)
     - `SceneTransition_EncounterToFleet_RendersFleetScene` — transitioning to fleet shows fleet content
     - `SceneTransition_FleetToCase_RendersCaseScene` — transitioning to case shows case content
     - `SceneTransition_PillNav_AllowsNonLinearNavigation` — can jump from encounter directly to case
   - File: `apps/dashboard/src/components/demo/__tests__/DemoProvider.test.tsx` (enhance from task-001)
     - `DemoProvider_TransitionToFleet_FromEhr_SetsScene` — EHR completion callback sets scene to fleet
     - `DemoProvider_AutoPlay_CyclesThroughScenes` — auto-play mode advances scenes on timer
   - Expected failure: Transition logic and auto-play not yet implemented

2. **[GREEN]** Implement transitions
   - File: `apps/dashboard/src/components/demo/SceneTransition.tsx` (enhance)
     - Add transition variants per scene pair:
       - Encounter→Fleet: scale-down/overlay/scale-up (800ms)
       - Fleet→Case: card-expand/fade (600ms)
       - Generic: slide-fade (500ms)
     - `layoutId` shared elements for PA status badge and case card
   - File: `apps/dashboard/src/components/demo/DemoProvider.tsx` (enhance)
     - Add `autoPlay: boolean, setAutoPlay` to context
     - Auto-play timer: advance scene every 15s when enabled
     - `triggerTransitionToFleet()` callback for EHR demo to invoke

3. **[REFACTOR]** Extract transition variants to `transitionVariants.ts` constants file

**Dependencies:** task-007, task-011, task-014 (all scenes must exist)
**Parallelizable:** No (integration task)

---

### Task 016: Integration polish and demo entry point
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test:
   - File: `apps/dashboard/src/routes/__tests__/ehr-demo.test.tsx` (enhance existing)
     - `EhrDemo_WithSceneNav_RendersSceneNavigation` — SceneNav appears at top of EHR demo
     - `EhrDemo_CompleteFlow_ShowsTransitionToFleetOption` — after PA complete, transition is available
     - `EhrDemo_ChartTabs_AreAccessible` — chart tab navigation works within enhanced EHR
   - Expected failure: EHR demo not yet wired to SceneNav/DemoProvider

2. **[GREEN]** Wire everything together
   - File: `apps/dashboard/src/routes/ehr-demo.tsx` (enhance)
     - Wrap in DemoProvider (or consume from parent layout)
     - Add SceneNav at top
     - Wire `useEhrDemoFlow` onTransition to scene change
     - Pass enhanced demo data to sidebar chart tabs
   - File: `apps/dashboard/src/routes/__root.tsx` or layout
     - Add DemoProvider wrapper for demo routes
   - Verify all three scenes navigate correctly via pill nav

3. **[REFACTOR]** Final cleanup of unused imports, dead code

**Dependencies:** task-015
**Parallelizable:** No (final integration)

---

## Group 6: Backend Seed Script (Independent)

### Task 017: PostgreSQL demo data seed script
**Phase:** RED → GREEN → REFACTOR

1. **[RED]** Write test:
   - File: `apps/gateway/Gateway.API.Tests/Data/SeedDemoDataTests.cs`
   - Tests (TUnit):
     - `SeedDemoData_GeneratesCorrectCount_48Requests` — seed generates 48 PA requests
     - `SeedDemoData_DistributesStatuses_PerSpec` — status distribution matches design (6 processing, 8 ready, etc.)
     - `SeedDemoData_UsesAllTestPatients` — all 7 test patient IDs appear
     - `SeedDemoData_IncludesMultipleProcedures` — CPT 72148, 72141, 74177 all present
     - `SeedDemoData_IncludesMultiplePayers` — Aetna, UHC, Cigna all present
     - `SeedDemoData_ConfidenceScoresInRange` — confidence scores between 60-98
     - `SeedDemoData_TimestampsSpreadOverWeek` — createdAt values span 7 days
   - Expected failure: SeedDemoData class does not exist

2. **[GREEN]** Implement seed script
   - File: `apps/gateway/Gateway.API/Data/SeedDemoData.cs`
     - Static method `GenerateSeedRequests(): List<PARequestModel>`
     - Builds 48 requests with realistic variety
     - Called from `Program.cs` when `ASPNETCORE_ENVIRONMENT=Demo`
   - File: `apps/gateway/Gateway.API/Program.cs` (enhance)
     - Add conditional seed call on startup

3. **[REFACTOR]** Extract patient/procedure fixtures to constants

**Dependencies:** None (backend, independent of frontend)
**Parallelizable:** Yes (fully independent from all frontend tasks)

---

## Parallelization Summary

```
Wave 1 (sequential):
  task-001 → task-002 → task-003

Wave 2 (3 parallel streams + 1 independent):
  Stream A: task-004 → task-005 → task-006 → task-007
  Stream B: task-008 → task-009 → task-010 → task-011
  Stream C: task-012 → task-013 → task-014
  Stream D: task-017 (backend, fully independent)

Wave 3 (sequential):
  task-015 → task-016
```

**Maximum parallelism:** 4 agents (Waves 2A + 2B + 2C + 2D)
**Critical path:** task-001 → 002 → 003 → 004 → 005 → 006 → 007 → 015 → 016

## Task Summary

| ID | Title | Dependencies | Parallel | Group |
|----|-------|-------------|----------|-------|
| task-001 | DemoProvider context + install deps | None | No | Foundation |
| task-002 | SceneNav pill navigation | task-001 | No | Foundation |
| task-003 | Route scaffolding + SceneTransition | task-002 | No | Foundation |
| task-004 | Fleet seed data + KPICards | task-003 | Yes | Fleet |
| task-005 | FleetCard + FleetView | task-004 | No | Fleet |
| task-006 | CasePipeline | task-004 | Yes | Fleet |
| task-007 | Fleet route assembly + GraphQL | task-005, task-006 | No | Fleet |
| task-008 | Enhanced demoData + ChartTabPanel | task-003 | Yes | EHR |
| task-009 | Enhanced EncounterSidebar | task-008 | No | EHR |
| task-010 | Enhanced useEhrDemoFlow state machine | task-003 | Yes | EHR |
| task-011 | Auth detection banner + EhrHeader | task-010 | No | EHR |
| task-012 | React Flow custom nodes | task-003 | Yes | Case |
| task-013 | CaseGraph + AnimatedEdge | task-012 | No | Case |
| task-014 | CaseTimeline + case route assembly | task-013 | No | Case |
| task-015 | Scene transitions + shared layout | task-007, task-011, task-014 | No | Integration |
| task-016 | Integration polish + demo entry | task-015 | No | Integration |
| task-017 | Backend seed script (C#) | None | Yes | Backend |
