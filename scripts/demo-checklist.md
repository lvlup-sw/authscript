# Demo Validation Checklist — "Maria's Story"

Run through this checklist before every demo to verify the full flow works.

## Pre-Demo Setup

- [ ] Start Aspire orchestrator — all services healthy (Dashboard :5173, Gateway :5000, Intelligence :8000)
- [ ] Verify demo data seeded — dashboard shows 5+ PA requests in queue
- [ ] Warm up Intelligence service — open any existing PA to confirm API responds

## EHR Demo Flow (`/ehr-demo`)

- [ ] Navigate to `http://localhost:5173/ehr-demo` — page loads in <2s
- [ ] Verify athenaOne header shows "Maria Garcia", DOB, MRN
- [ ] Verify encounter note shows Chief Complaint, HPI, Assessment, Plan
- [ ] Verify "Sign Encounter" button is green and clickable

## Sign → Process Flow

- [ ] Click "Sign Encounter" — button changes to "Encounter Signed" (disabled)
- [ ] AuthScript iframe appears below with smooth transition
- [ ] NewPAModal auto-opens at confirm step showing Rebecca Sandbox + MRI Lumbar Spine
- [ ] Click "Request PA" — processing animation plays (4 steps with icons)
- [ ] Processing completes — success screen with checkmark

## Review Flow

- [ ] Click "Review PA Request" — analysis detail page loads
- [ ] Confidence ring shows ≥85% (green)
- [ ] Criteria section shows all criteria met (green checks)
- [ ] Click any criterion — reasoning modal appears with evidence
- [ ] Close modal — return to analysis page
- [ ] Clinical summary shows AI-generated text with "AI auto-filled" badge

## Submit Flow

- [ ] Click "Confirm & Submit" — submission overlay appears
- [ ] Two-phase animation: "Locating submission method..." → "Found [payer] ePA"
- [ ] Success modal: checkmark + "PA Request Submitted"
- [ ] Click "Back to Dashboard" — returns to main dashboard

## Timing Validation

- [ ] Full flow completes in 60–90 seconds
- [ ] No visible errors, loading spinners stuck, or broken states
- [ ] Run 3 consecutive demos — all succeed consistently

## Quick Demo Shortcut (alternative flow)

If skipping the EHR stub:
- [ ] Navigate to `http://localhost:5173/?quickDemo=true`
- [ ] NewPAModal auto-opens at confirm step
- [ ] Proceed from "Request PA" through submit (same as above)
