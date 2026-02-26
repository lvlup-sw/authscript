# Design: EHR Mock Polish — athenaOne Fidelity Upgrade

**Feature ID:** `ehr-mock-polish`
**Date:** 2026-02-25
**Type:** Enhancement (demo fidelity)

## Problem

The current `/ehr-demo` page is a flat encounter note with a sign button. It doesn't look or feel like a real EHR encounter. Investor audiences familiar with healthcare IT will notice the gap. Three specific issues:

1. **No encounter workflow** — Real athenaOne has a staged workflow sidebar (Review → HPI → ROS → PE → A&P → Sign-Off). Our page is a single scrollable card.
2. **Flat note layout** — Real encounters use collapsible section cards with metadata (provider, date, vitals). Ours is a plain card with text sections.
3. **Data inconsistency** — The EHR shows "Maria Garcia" but the PA system processes "Rebecca Sandbox". The clinical note says "45-year-old" but the DOB makes her 44.

## Constraints

- Must remain a lightweight mock (no real athenaOne SDK integration)
- All changes are UI-only within the dashboard app
- Must not break the existing demo flow (`?quickDemo=true` path)
- Must remain visually convincing at demo pace (60–90 seconds)

## Solution

### 1. Encounter Workflow Sidebar

Add a left sidebar mimicking athenaOne's encounter stage navigation. Stages: **Review → HPI → ROS → PE → A&P → Sign-Off**. The A&P stage is active by default (this is where the provider orders the MRI and AuthScript launches).

The sidebar is purely visual — clicking stages does not navigate. The active stage is highlighted. "Sign-Off" becomes clickable after the encounter is signed.

```
┌────────────────────────────────────────────────────────┐
│  athenaOne  │ Charts │ Schedule │ Messages │ Admin     │
├──────────┬─────────────────────────────────────────────┤
│ Encounter│  Maria Garcia  DOB: 03/15/1981  MRN: 60182 │
│          │  Dr. Sarah Chen · Family Medicine            │
│ □ Review │  02/25/2026 · Office Visit                   │
│ □ HPI    ├──────────────────────────────────────────────┤
│ □ ROS    │                                              │
│ □ PE     │  ┌─ Assessment & Plan ─────────────────────┐ │
│ ■ A&P ◄──│  │ Dx: M54.5 Low back pain                │ │
│ ○ Sign   │  │     M54.51 Lumbar radiculopathy, left   │ │
│          │  │ Plan: Order MRI lumbar spine w/o contrast│ │
│          │  └─────────────────────────────────────────┘ │
│          │                                              │
│          │  ┌─ Orders ────────────────────────────────┐ │
│          │  │ MRI Lumbar Spine w/o Contrast (72148)   │ │
│          │  │ Status: Requires Prior Authorization     │ │
│          │  └─────────────────────────────────────────┘ │
│          │                                              │
│          │  [Sign Encounter]                            │
│          │                                              │
│          │  ┌─ AuthScript (Encounter Card) ───────────┐ │
│          │  │ (appears after signing)                  │ │
│          │  └─────────────────────────────────────────┘ │
└──────────┴──────────────────────────────────────────────┘
```

**Components:**
- `EncounterSidebar` — Stage list with active/completed/pending states
- Update `ehr-demo.tsx` to use a two-column layout

### 2. Encounter Card Layout + Metadata

Replace the flat encounter note with structured cards and add encounter metadata:

**Encounter metadata bar** (below patient banner):
- Provider: "Dr. Sarah Chen, MD"
- Specialty: "Family Medicine"
- Date: "02/25/2026"
- Type: "Office Visit"

**Collapsible section cards** replacing the flat note:
- CC/HPI card (collapsed by default — already "completed" in the A&P stage)
- Assessment & Plan card (expanded — active stage)
- Orders card (new) — shows the MRI order with "Requires Prior Authorization" status badge

**Vitals row** (static, below metadata):
- BP: 128/82 · HR: 72 · Temp: 98.6°F · SpO2: 99%

### 3. Data Consistency

- Change `DEMO_EHR_PATIENT` from "Maria Garcia" to "Rebecca Sandbox" — unify identity across EHR and PA
- Update DOB to "09/14/1990" to match the Intelligence fixture
- Update MRN to "ATH60182"
- Fix clinical note age reference to match DOB (35 years old as of 2026-02-25)
- Update DEMO_ENCOUNTER text to be consistent with `demo_mri_lumbar.json` fixture

### 4. CSS Polish

- Move inline `@keyframes fadeSlideIn` to Tailwind config or a CSS module
- Add loading skeleton for the iframe while it loads
- Ensure responsive behavior at common demo screen sizes (1280×720, 1920×1080)

## Components Summary

| Component | Status | Changes |
|-----------|--------|---------|
| `EhrHeader` | Modify | Add encounter metadata row |
| `EncounterNote` | Rewrite | Collapsible cards, vitals row, orders card |
| `EncounterSidebar` | **New** | Workflow stage navigation |
| `SignEncounterButton` | Keep | No changes |
| `EmbeddedAppFrame` | Modify | Add loading skeleton |
| `ehr-demo.tsx` | Modify | Two-column layout, sidebar integration |
| `demoData.ts` | Modify | Unify patient identity, add vitals/orders data |

## Success Criteria

- [ ] Sidebar shows 6 encounter stages with A&P highlighted
- [ ] Encounter metadata displays provider, date, visit type
- [ ] Vitals row visible below metadata
- [ ] Assessment & Plan card is expanded; CC/HPI collapsed
- [ ] Orders card shows MRI with "Requires PA" badge
- [ ] Patient identity is "Rebecca Sandbox" throughout (EHR + PA)
- [ ] Age in clinical note matches DOB
- [ ] Inline keyframe animation removed from JSX
- [ ] Iframe shows loading skeleton before content loads
- [ ] Full demo flow works end-to-end with new layout
