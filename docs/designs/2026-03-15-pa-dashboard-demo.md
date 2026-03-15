# Design: Scene-Based PA Demo with Cinematic Transitions

**Feature ID:** `pa-dashboard-demo`
**Date:** 2026-03-15
**Status:** Draft

## Problem Statement

Our current demo has two disconnected experiences: a linear EHR encounter flow (`/ehr-demo`) and a utilitarian ops dashboard (`/`). Neither individually communicates the full product vision to our two target audiences — clinicians who need to see PA working inside their EHR, and investors/executives who need to see the fleet-level "command center" story.

The Palantir Prior Auth Hub demo (see `docs/market-research/palantir-dashboard.md`) shows what enterprise PA visualization looks like — animated fleet views, KPI cards, case drilldowns with progress graphs. But their approach is standalone, outside the EHR. Our differentiator is that we're **embedded inside the EHR**. We need to show both stories — the clinical embed AND the ops intelligence — connected by cinematic transitions that make the demo feel like a product, not a prototype.

## Design Overview

A three-scene demo experience connected by Framer Motion transitions:

```
Scene 1: "The Encounter"          Scene 2: "The Command Center"       Scene 3: "The Case Detail"
┌─────────────────────────┐       ┌──────────────────────────┐       ┌──────────────────────────┐
│ athenaOne EHR Mock      │       │ KPI Cards (animated)     │       │ Case Progress Graph      │
│ ┌─────┬───────────────┐ │       │ ┌──┐ ┌──┐ ┌──┐ ┌──┐     │       │    ○──○──●──○──○         │
│ │Chart│ Encounter     │ │  ──►  │ └──┘ └──┘ └──┘ └──┘     │  ──►  │                          │
│ │Tabs │ Sections      │ │ zoom  │ Fleet View (48 cases)    │ drill │ Node Graph (React Flow)  │
│ │     │ + PA Widget   │ │  out  │ ┌─┬─┬─┬─┬─┬─┬─┬─┐      │ down  │ Patient → Evidence →     │
│ │     │               │ │       │ └─┴─┴─┴─┴─┴─┴─┴─┘      │       │ Criteria → Decision      │
│ └─────┴───────────────┘ │       │ Case Progress Pipeline   │       │                          │
└─────────────────────────┘       └──────────────────────────┘       └──────────────────────────┘
```

**Navigation:** A minimal top bar with scene pills (`Encounter | Fleet | Case`) allows non-linear navigation for flexible presenting.

## Scene 1: "The Encounter" — Fully Mocked athenaOne EHR

### Design Goal
Make it look like a real athenaHealth encounter. Not a pixel-perfect clone, but convincing enough that a clinician says "oh, that's athena."

### athenaOne Reference Architecture
Based on athenahealth's FHIR encounter section ValueSet and product documentation, an athenaOne encounter contains:

**Encounter Sections** (from `ah-encounter-section-item` ValueSet):
- `HPINote` / `HPITemplate` — History of Present Illness
- `ROSNote` / `ROSTemplate` — Review of Systems
- `PENote` / `PETemplate` — Physical Exam
- `APNote` — Assessment & Plan

**Encounter Stages** (from athenaOne workflow documentation):
- Intake → Exam → Orders → Sign → Checkout

**Patient Chart Tabs** (from athenaOne charting features):
- Problems (organized by specialty categories)
- Medications (with reconciliation)
- Allergies
- Vitals
- Imaging History
- Lab Results
- Documents

**Authorization Features** (from athenaPayer / Express Authorizations):
- Authorization Determination Engine — auto-detects PA requirements at order entry
- Authorization Tracker — monitors authorization status at a glance
- Direct payer integration — surfaces PA stages within native clinical workflows

### Implementation

#### EHR Chrome (Enhanced from current)

**Header** (`EhrHeader.tsx` — enhance existing):
- Keep current dark blue (`#1a365d`) athenaOne-style branding
- Add: Encounter type pill ("Office Visit"), Facility name ("Family Care Associates")
- Add: Clock/timer showing encounter duration (cosmetic)

**Left Sidebar** (replace current `EncounterSidebar.tsx`):
Split into two sections:

1. **Patient Chart Tabs** (top, always visible):
   - Problems | Meds | Allergies | Vitals | Imaging | Labs
   - Each tab shows a mini summary panel when clicked (static mock data)
   - Problems tab: 3-4 items grouped by category (Musculoskeletal: "M54.5 Low back pain", "M54.41 Lumbago with sciatica")
   - Meds tab: Current medications list (Ibuprofen 800mg, Cyclobenzaprine 10mg)
   - Allergies tab: "NKDA" or 1-2 entries
   - Imaging tab: "No prior lumbar imaging" (important — establishes no duplicative imaging for LCD criteria)

2. **Encounter Stage Tracker** (bottom):
   - Vertical stepper: Intake (done) → HPI (done) → ROS (done) → PE (done) → A&P (active) → Orders → Sign
   - Stage indicators match current design (green check, teal active, gray pending)
   - When PA detected: additional stages appear below Orders (Analyzing → Review → Submit)

**Main Content** (`EncounterNote.tsx` — enhance existing):
Keep current structure but add richer encounter sections:

- **Vitals Bar**: (keep current) BP, HR, Temp, SpO2, Weight, Height, BMI
- **Chief Complaint / HPI**: (keep current expandable section)
  - Show the HPI text with documentation quality indicators
  - AI suggestion box for gap documentation (keep current)
- **Assessment & Plan**: (keep current)
  - Orders card with PA badge
  - Show "MRI Lumbar Spine w/o Contrast (CPT 72148)" with amber "Requires PA" badge

**New: Authorization Detection Moment**:
When the provider adds the MRI order, animate an "Authorization Determination Engine" notification:
- Slide-in notification bar: "PA Required — Aetna LCD L34220 applies to CPT 72148"
- This mimics athenaOne's Authorization Determination Engine behavior
- Transitions the sidebar to show PA stages

**PA Widgets** (keep and polish existing):
- `PAReadinessWidget.tsx` — Pre-sign criteria checklist (keep current)
- `PAResultsPanel.tsx` — Post-sign processing animation (keep current)
- Add: "Express Authorization" branding on the PA panel header to reference athenaOne's actual feature name

#### Demo Flow Enhancement

Current flow: `idle → flagged → signing → processing → reviewing → submitting → complete`

Enhanced flow:
```
chart-browsing → order-entry → pa-detected → documenting → flagged → signing → processing → reviewing → submitting → complete → transition-to-fleet
```

New states:
- `chart-browsing`: Provider reviews chart tabs (Problems, Meds, etc.)
- `order-entry`: Provider adds MRI order, triggers Authorization Determination Engine
- `pa-detected`: Notification slides in, sidebar updates with PA stages
- `transition-to-fleet`: After completion, animated transition to Scene 2

### Data Requirements
- Enhance `demoData.ts` with:
  - Problem list entries (M54.5, M54.41, M79.3)
  - Medication list (Ibuprofen, Cyclobenzaprine, Gabapentin)
  - Allergy data (NKDA)
  - Prior imaging history (empty — supports "no duplicative imaging" criterion)
  - Lab results (optional, 1-2 entries for realism)

---

## Scene 2: "The Command Center" — Fleet Dashboard

### Design Goal
Palantir-level visual impact. Animated fleet of in-flight PA cases with live KPI cards and a case progress pipeline. This scene answers: "what does the system look like at scale?"

### Implementation

#### Route & Data Strategy
- Route: enhance existing `/` (index.tsx) or create `/fleet` dedicated demo route
- Pre-seed PostgreSQL with ~48 mock PA requests across all stages via a seed script
- Use existing GraphQL queries (`GET_PA_REQUESTS`, `GET_PA_STATS`, `GET_ACTIVITY`) — no new backend work
- Auto-refresh every 5s (already implemented in React Query)

#### Layout

**Scene Header**:
- Title: "Prior Authorization Command Center" with "Family Care Associates" subtitle
- Live status indicator (green dot + "System Operational")
- Filter bar: Facility, Date Range, Payer, Provider (cosmetic filters)

**KPI Cards Row** (enhance existing `StatCard`):
6 cards in a horizontal grid, each with:
- Large animated counter (count-up animation on mount)
- Label and trend indicator
- Color-coded top border accent
- Cards: Total (48, teal), Processing (12, blue), Ready for Review (8, purple), Submitted (15, amber), Approved (11, green), Denied (2, red)
- Clicking a KPI card filters the fleet view below

**Fleet View** (new component — `FleetView.tsx`):
The centerpiece. An animated grid of case cards representing all in-flight PA cases.

Each case card (~120x80px):
- Patient initials avatar (colored by status)
- Status dot (pulsing for active states)
- Procedure code (small text)
- Payer badge
- Confidence indicator (if analyzed)
- Subtle glow effect on the case from Scene 1

Grid behavior:
- Cards sort/filter by status with layout animation (Framer Motion `layout` prop)
- New cases animate in with `scaleIn` effect
- Status changes trigger a brief pulse animation
- The case from Scene 1 (Rebecca Sandbox) enters with a distinctive glow/highlight

**Case Progress Pipeline** (enhance existing `WorkflowProgress.tsx`):
Horizontal pipeline showing aggregate flow:
```
Order Signed → PA Detected → AI Processing → Ready for Review → Submitted → Payer Response
    [12]           [8]           [6]              [8]             [15]          [13]
```
- Each stage shows count of cases currently in that stage
- Animated dots flow between stages (CSS animation along SVG path)
- Clicking a stage filters the fleet view

**Activity Feed** (keep existing, enhance):
- Right sidebar showing real-time activity stream
- "Rebecca Sandbox — PA Submitted to Aetna — 2 min ago" appears after Scene 1

#### Animation Specifications

**KPI Counter Animation**:
- Count-up from 0 to target value over 800ms
- Easing: `easeOut`
- Stagger: 100ms between cards (left to right)

**Fleet Card Entry**:
- Initial: `{ opacity: 0, scale: 0.8, y: 20 }`
- Animate: `{ opacity: 1, scale: 1, y: 0 }`
- Stagger: 30ms between cards
- Duration: 400ms

**Pipeline Dots**:
- CSS `@keyframes` animation along SVG `offset-path`
- Duration: 3s per dot, continuous loop
- Opacity: 0.6, pulsing

---

## Scene 3: "The Case Detail" — Drilldown with Node Graph

### Design Goal
Show the AI reasoning visually. A node graph that makes the invisible (policy analysis, evidence extraction, criteria evaluation) visible and tangible. This is the "how does it work?" scene.

### Implementation

#### Route
- Enhance existing `/analysis/$transactionId` page, or create a dedicated demo variant
- Accessible by clicking any case in Scene 2's fleet view

#### Layout

**Split Layout**: Left panel (55%) for node graph, Right panel (45%) for detail cards.

**Left Panel: Case Anatomy Graph** (new component — `CaseGraph.tsx`):
Built with `@xyflow/react` (React Flow v12).

Node types:
1. **Patient Node** (top center):
   - Patient name, DOB, MRN, insurance
   - Avatar with status ring
   - Color: teal border

2. **Clinical Evidence Nodes** (left cluster):
   - One node per piece of extracted evidence
   - "HPI: Chronic LBP with radiculopathy, 6 months"
   - "Assessment: Lumbar radiculopathy L5-S1"
   - "Conservative Tx: 8 weeks PT, 6 weeks NSAIDs"
   - "No prior lumbar imaging documented"
   - Color: blue/slate

3. **Policy Criteria Nodes** (center column):
   - One node per LCD criterion
   - Shows criterion name + MET/NOT_MET/INDETERMINATE status
   - Met = green border + check, Not met = red border + X, Indeterminate = amber border + ?
   - "Valid ICD-10 for lumbar pathology" ✓
   - "Red flag symptoms or neuro deficit" ✓
   - "4+ weeks conservative management" ✓
   - "Clinical rationale documented" ✓
   - "No recent duplicative imaging" ✓

4. **Payer Decision Node** (right):
   - Shows payer name (Aetna), policy reference (LCD L34220)
   - Overall confidence score (93%)
   - Animated confidence ring (builds up on mount)
   - Status: "Ready for Submission" / "Submitted" / "Approved"

Edge types:
1. **Evidence → Criteria edges**: Animated dashed lines showing which evidence supports which criterion. SVG `<animateMotion>` dots flow from evidence to criteria nodes.
2. **Criteria → Decision edges**: Solid lines, green for met criteria, red for not met. Animated on mount with staggered timing.
3. **Patient → Evidence edges**: Subtle connecting lines showing data extraction from patient chart.

Animation sequence (on mount, 3s total):
1. Patient node fades in (0-300ms)
2. Evidence nodes cascade in from left (300-900ms, staggered 100ms each)
3. Evidence→Criteria edges animate with flowing dots (900-1800ms)
4. Criteria nodes light up green one by one (1800-2400ms, staggered 120ms each)
5. Criteria→Decision edges draw in (2400-2700ms)
6. Decision node scales in with confidence ring animation (2700-3000ms)

**Right Panel: Detail Cards**:

1. **Case Progress Timeline** (new component — `CaseTimeline.tsx`):
   - Vertical timeline showing phase transitions with timestamps
   - Past phases: solid line, gray text, checkmark
   - Current phase: pulsing dot, bold text, active color
   - Future phases: dashed line, muted text
   ```
   ✓ Order Signed ──── 2:34 PM
   ✓ PA Detected ──── 2:34 PM (< 1s)
   ✓ AI Processing ── 2:34 PM (4.2s)
   ✓ Review Ready ─── 2:35 PM
   ● Submitted ────── 2:36 PM (reviewer: 48s)
   ○ Payer Response ─ pending
   ```

2. **Patient Summary Card**:
   - Compact patient demographics
   - Insurance details
   - Referring provider

3. **Criteria Evidence Card** (enhance existing `EvidencePanel.tsx`):
   - Expandable accordion per criterion
   - Shows evidence text with source highlighting
   - Confidence per criterion

4. **Clinical Summary Card**:
   - AI-generated narrative summary
   - Extracted from Intelligence service response

---

## Transitions

### Tech: Framer Motion (`motion/react`)

Since we use TanStack Router (not Next.js App Router), we implement transitions via a `SceneTransition` wrapper component using `AnimatePresence` and `motion.div`.

### Scene Navigation Component (`SceneNav.tsx`)

```
┌──────────────────────────────────────────────────────────┐
│  ○ Encounter    ○ Fleet    ○ Case Detail     [Demo Controls] │
└──────────────────────────────────────────────────────────┘
```
- Pill-style scene selector, always visible at top
- Active scene has filled pill
- Demo Controls: Reset Demo, Auto-Play toggle, Speed selector

### Transition 1: Encounter → Fleet ("Zoom Out")

**Trigger**: PA submission completes in Scene 1, or user clicks "Fleet" pill.

**Animation** (800ms):
1. EHR content scales down: `scale: 1 → 0.85`, `opacity: 1 → 0`
2. Brief black/dark overlay: `opacity: 0 → 0.3 → 0` (200ms pulse)
3. Fleet dashboard scales up from center: `scale: 0.9 → 1`, `opacity: 0 → 1`
4. KPI counters begin count-up animation
5. Fleet cards stagger in
6. Rebecca Sandbox's case card pulses with teal glow

**Shared element**: The PA status badge from Scene 1 uses `layoutId="pa-status"` to animate seamlessly into the corresponding fleet card's status dot.

### Transition 2: Fleet → Case Detail ("Drill Down")

**Trigger**: User clicks a case card in Scene 2, or user clicks "Case Detail" pill.

**Animation** (600ms):
1. Clicked card scales up: `scale: 1 → 1.1` with elevation increase
2. Other fleet cards fade: `opacity: 1 → 0` (200ms)
3. Card expands to fill screen using `layoutId` shared layout animation
4. Node graph and detail panels slide in from edges
5. Graph animation sequence begins (see Scene 3 animation spec)

### Transition 3: Any → Any (via pill navigation)

**Animation** (500ms):
- Outgoing scene: `opacity: 1 → 0`, `x: 0 → -30px`
- Incoming scene: `opacity: 0 → 1`, `x: 30px → 0`
- Easing: `[0.4, 0, 0.2, 1]` (Material Design standard easing)

---

## New Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `motion` | ^12 | Page transitions, micro-animations, layout animations (successor to framer-motion) |
| `@xyflow/react` | ^12 | Node graph visualization in Scene 3 |

Both are MIT licensed, well-maintained, and widely used in production React apps.

**Not adding**: d3, recharts, GSAP, react-spring. Motion + React Flow cover all animation and visualization needs.

---

## Data Seeding

### Mock PA Request Seed Script

Create `apps/gateway/Gateway.API/Data/SeedDemoData.cs` that populates PostgreSQL with ~48 PA requests:

**Distribution across stages**:
- `processing`: 6 cases
- `ready`: 8 cases
- `submitted`: 15 cases (submitted today)
- `waiting_for_insurance`: 8 cases
- `approved`: 9 cases
- `denied`: 2 cases

**Data variety**:
- Use all 7 test patients (Donna, Eleana, Frankie, Anna, Rebecca, Gary, Dorrie)
- Multiple procedure types: MRI (CPT 72148, 72141), CT (CPT 74177), Surgery referrals
- Multiple payers: Aetna, United Healthcare, Cigna
- Confidence scores: 60-98% range
- Timestamps: spread over last 7 days

**Trigger**: Seed runs on startup when `ASPNETCORE_ENVIRONMENT=Demo` or via a `/api/seed-demo` endpoint.

---

## File Structure

```
apps/dashboard/src/
├── routes/
│   ├── ehr-demo.tsx              # Enhanced Scene 1 (modify existing)
│   ├── fleet.tsx                 # Scene 2: Command Center (new)
│   └── case.$caseId.tsx          # Scene 3: Case Detail (new)
├── components/
│   ├── demo/
│   │   ├── SceneNav.tsx          # Top pill navigation between scenes
│   │   ├── SceneTransition.tsx   # AnimatePresence wrapper for scene changes
│   │   └── DemoProvider.tsx      # Context: current scene, demo state, shared data
│   ├── ehr/
│   │   ├── EhrHeader.tsx         # Enhanced (minor additions)
│   │   ├── EncounterSidebar.tsx  # Rewritten (chart tabs + encounter stages)
│   │   ├── ChartTabPanel.tsx     # New: mini summary panels for chart tabs
│   │   ├── EncounterNote.tsx     # Enhanced (order entry + auth detection)
│   │   ├── PAReadinessWidget.tsx # Keep existing
│   │   ├── PAResultsPanel.tsx    # Keep existing
│   │   └── useEhrDemoFlow.ts    # Enhanced state machine (new states)
│   ├── fleet/
│   │   ├── FleetView.tsx         # New: animated case card grid
│   │   ├── FleetCard.tsx         # New: individual case card
│   │   ├── KPICards.tsx          # New: animated KPI counter row
│   │   └── CasePipeline.tsx     # New: enhanced workflow pipeline with counts
│   └── case/
│       ├── CaseGraph.tsx         # New: React Flow node graph
│       ├── CaseTimeline.tsx      # New: vertical phase timeline
│       ├── PatientNode.tsx       # New: React Flow custom node
│       ├── EvidenceNode.tsx      # New: React Flow custom node
│       ├── CriteriaNode.tsx      # New: React Flow custom node
│       ├── DecisionNode.tsx      # New: React Flow custom node
│       └── AnimatedEdge.tsx     # New: React Flow custom animated edge
└── lib/
    ├── demoData.ts              # Enhanced (chart tab data, more fixtures)
    └── fleetSeedData.ts         # New: mock fleet data for client-side fallback
```

---

## Scoping & Prioritization

### Phase 1: Foundation (P0)
1. Install `motion` and `@xyflow/react`
2. Build `SceneNav` and `SceneTransition` components
3. Build `DemoProvider` context
4. Create `/fleet` and `/case/$caseId` routes with placeholder content

### Phase 2: Scene 2 — Fleet Dashboard (P0)
5. Build `KPICards` with count-up animation
6. Build `FleetView` with animated card grid
7. Build `CasePipeline` with flowing dots
8. Wire to existing GraphQL queries
9. Create seed script for 48 mock requests

### Phase 3: Scene 1 — Enhanced EHR (P0)
10. Add chart tab navigation to sidebar
11. Build `ChartTabPanel` with static mock data
12. Add order-entry and auth-detection states to flow
13. Add transition trigger on completion

### Phase 4: Scene 3 — Case Detail Graph (P1)
14. Build React Flow custom nodes (Patient, Evidence, Criteria, Decision)
15. Build `AnimatedEdge` with SVG motion
16. Build `CaseGraph` with sequenced mount animation
17. Build `CaseTimeline` vertical timeline

### Phase 5: Transitions & Polish (P1)
18. Implement Scene 1→2 zoom-out transition with shared layout
19. Implement Scene 2→3 drill-down transition
20. Auto-play mode for unattended demo
21. Polish animations and timing

---

## Success Criteria

1. **Clinician believability**: A provider watching Scene 1 recognizes it as "an EHR encounter" without being told
2. **Investor impact**: Scene 2's fleet view elicits a "wow" reaction — animated, data-rich, clearly at-scale
3. **Technical clarity**: Scene 3's node graph makes AI reasoning visible and understandable
4. **Smooth transitions**: Scene changes feel cinematic, not jarring — no flicker, no layout jump
5. **Demo flexibility**: Presenter can navigate scenes non-linearly via pill nav
6. **Data realism**: 48 pre-seeded cases with realistic variety in payers, procedures, confidence scores

## Research Sources

- [athenahealth AI-Native Clinical Encounter (Nov 2025)](https://hlth.com/insights/news/athenahealth-introduces-ai-native-clinical-encounter-to-redefine-the-ehr-experience-2025-11-05)
- [athenahealth FHIR Encounter Section ValueSet](https://fhir.athena.io/athenacoreext/ValueSet-ah-encounter-section-item.html)
- [athenaPayer: Modernizing Prior Authorization (Dec 2025)](https://www.athenahealth.com/resources/blog/athenapayer-modernizing-prior-authorization)
- [athenaOne Authorization Management Solutions](https://www.athenahealth.com/solutions/athenaone/authorization-management)
- [athenaOne Charting Features (Dec 2025)](https://www.athenahealth.com/resources/blog/5-ways-athenaone-makes-charting-easier)
- [Palantir AIP Healthcare Utilization Review](https://aip.palantir.com/workflow/14128D7B-5855-4907-948A-67D9264ABD90)
- [React Flow (xyflow) Documentation](https://reactflow.dev)
- [Motion for React — AnimatePresence](https://motion.dev/docs/react-animate-presence)
- [Motion for React — Layout Animations](https://motion.dev/docs/react-layout-animations)
