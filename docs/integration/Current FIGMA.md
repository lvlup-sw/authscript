### Current FIGMA

> ⚠️ **Reconciliation Note (2026-02-01):** This document describes the product team's original vision. See the [Technical Reconciliation](#technical-reconciliation) section at the end for MVP scope adjustments based on athenahealth API constraints.

---

## Original Vision

### **1. Core Workflow Architecture**

The system logic follows a "Triage & Treat" pattern:

1. **Ingestion:** The system pulls orders from the EMR (Athenahealth ADT Feed).
2. **AI Analysis:** It runs an inference to check medical necessity against payer guidelines.
3. **Routing:**
   - **High Confidence (>95%):** Routed to **Clinical Queue** for quick approval.
   - **Low Confidence / Missing Data:** Routed to **Exception Handling** for manual intervention.
4. **Export:** Once approved/resolved, the auth is synced back to the EMR.

------

### **2. Screen-by-Screen Behavior Outline**

#### **A. Dashboard (The "Air Traffic Control" View)**

- **Goal:** Provide immediate system health visibility and operational metrics.
- **Key Behaviors:**
  - **Real-Time Status:** The blue banner indicates a live connection to the `Athenahealth ADT Feed`. If this connection drops, the system likely alerts the user immediately.
  - **Pipeline Visualization:** The cards (Green, Yellow, Gray) function as a Kanban-style summary. Clicking any card filters the view to that specific queue.
  - **Velocity Tracking:** The "Avg. Processing Time (2.1 days)" is a key KPI, likely calculated from *Order Received Timestamp* vs. *Authorization Submission Timestamp*.

#### **B. Clinical Queue (The "Happy Path")**

- **Goal:** Allow clinicians to batch-approve high-confidence AI drafts to maximize revenue speed.
- **Key Behaviors:**
  - **Confidence Thresholding:** The "Confidence Score" (e.g., 98%) implies the AI has matched clinical notes to insurance policies. Users will likely scan the green bars and approve these quickly.
  - **Financial Prioritization:** The "Revenue at Risk" metric suggests the table can be sorted by dollar amount ($), allowing the practice to prioritize high-value procedures (e.g., Remicade Infusion @ $3,200) over lower-value ones.
  - **Action:** Although not explicitly shown, the row behavior likely supports a "One-Click Approve" or a "Review Details" expansion to verify the AI's generated draft before submission.

#### **C. Exception Handling (The "Human-in-the-loop" Path)**

- **Goal:** resolving blockers that the AI cannot handle autonomously (e.g., missing lab results).
- **Key Behaviors:**
  - **Root Cause Identification:** The "Status" and "Issue" columns (e.g., "Missing TB Test") clearly define *why* the automation stopped.
  - **The "Resolve" Action:** Clicking the orange **Resolve** button likely triggers one of two flows:
    1. **Data Upload:** A modal to upload the missing document (e.g., the TB test result).
    2. **Task Assignment:** Pinging a nurse or medical assistant to schedule the missing test.
  - **Urgency Sorting:** The "Urgent" tag (e.g., for Stelara) likely bubbles time-sensitive treatments to the top to prevent care delays.

#### **D. Completed/Exported (The Audit Trail)**

- **Goal:** Confirmation of success and historical record keeping.
- **Key Behaviors:**
  - **Immutable Record:** Once an item is here, it is read-only.
  - **Sync Confirmation:** The status "Synced to EMR" confirms the prior auth number has been written back to the patient's chart in Athenahealth, closing the loop.

------

### **3. UX & Design Observations**

- **Revenue-Centric UI:** By prominently displaying "Revenue at Risk" on almost every screen, the design aligns clinical operations with business health. This is a strong persuasive element for administrative stakeholders.
- **Visual Hierarchy:** The use of traffic light colors (Green = Good/Go, Yellow = Warning/Pause, Red/Orange = Urgent) makes the dashboard scannable in seconds.
- **Drill-Down Filtering:** The metrics at the top of the queues (e.g., "Total Cases: 6") likely act as quick filters.

### **4. Gap Analysis (Implicit Interactions)**

To make this a fully functional prototype, you may need to define these "missing" states:

- **The "Review" View:** What happens when I click a row in the Clinical Queue? Does it open a side panel showing the AI-generated draft letter next to the patient notes?
- **The "Resolve" Interaction:** When clicking "Resolve" on a missing lab result, does the system let the user query the EMR for that result, or does the user have to leave the app to find it?

---

## Technical Reconciliation

> **Date:** 2026-02-01
> **Related:** [API Constraints Discovery](../debugging/2026-02-01-athenahealth-api-constraints.md) | [MVP Design](../designs/2026-01-29-athenahealth-pivot-mvp.md)

### Critical Conflicts Identified

| Figma Assumption | Technical Reality | Impact |
|------------------|-------------------|--------|
| ADT Feed (automatic ingestion) | athenahealth Certified API does not support global queries | ❌ **Not possible in MVP** |
| Practice-wide queue (6+ patients) | SMART launch provides ONE patient context at a time | ❌ **Not possible in MVP** |
| Revenue at Risk metrics | No pricing/reimbursement data available | ❌ **Deferred** |
| Batch approval (checkboxes) | Single patient context only | ❌ **Deferred** |
| "Synced to EMR" status | PDF to DocumentReference | ✅ **Compatible** |
| "Resolve" missing data flow | "Update with New Data" button | ✅ **Compatible** |

### Why the Original Vision Won't Work

**1. ADT Feed vs SMART Launch**

The Figma assumes:
> "The system pulls orders from the EMR (Athenahealth ADT Feed)"

Technical reality:
- athenahealth Certified FHIR R4 API **does not support global queries**
- Every resource query requires patient-specific identifiers
- Cannot "watch" for new encounters/orders across the practice
- ADT feeds require premium tier or proprietary API access

**2. Practice-Wide Queue vs Patient Context**

The Figma shows a dashboard with ALL pending PA requests across the practice. Technical reality:
- SMART launch provides ONE patient's context at a time
- Dashboard is embedded in patient chart iframe
- No way to query "all patients with pending PAs"

### MVP Scope (Patient-Centric Design)

The MVP reframes AuthScript as a **patient-specific PA assistant** rather than a practice-wide queue manager.

#### What's IN Scope

| Feature | Implementation |
|---------|----------------|
| Patient PA Status | Single-patient view showing current PA request status |
| Confidence/Completeness Indicator | Binary: complete or missing data |
| Evidence Panel | Source citations from clinical data |
| Form Preview | Editable PA form fields |
| Approve Action | Single-click approve → PDF generation |
| Missing Data Resolution | "Update with New Data" triggers re-hydration |
| EMR Write-back | PDF to DocumentReference |

#### What's DEFERRED (Post-MVP)

| Feature | Why Deferred | When Available |
|---------|--------------|----------------|
| Multi-patient queue view | Requires global query capability | When premium API tier available |
| ADT Feed auto-ingestion | Requires ADT subscription | When premium API tier available |
| Revenue at Risk metrics | Requires pricing data integration | TBD |
| Batch approval | Requires multi-patient context | When queue view available |
| Pipeline visualization | Requires practice-wide data | When queue view available |

### MVP Screen Mapping

#### Dashboard "Air Traffic Control" → Patient PA Status

| Original Element | MVP Status | Notes |
|------------------|------------|-------|
| Blue "ADT Feed Connected" banner | ❌ Remove | No ADT feed |
| Green/Yellow/Gray queue cards | ⚠️ Modify | Show status of CURRENT patient only |
| "Avg Processing Time" metric | ⚠️ Modify | Show for this patient only |
| Pipeline visualization | ❌ Remove | No practice-wide pipeline |
| "Total Cases: 6" aggregate | ❌ Remove | Single patient context |

#### Clinical Queue Table → Patient Review View

| Original Element | MVP Status | Notes |
|------------------|------------|-------|
| Multi-patient table | ❌ Remove | Single patient context |
| Patient Name column | ✅ Keep | Show current patient header |
| Procedure column | ✅ Keep | From ServiceRequest |
| Confidence Score | ✅ Keep | From AI completeness check |
| Revenue at Risk | ❌ Remove | No pricing data |
| Approve action | ✅ Keep | Single-click approve |

#### Exception Handling Table → Missing Data View

| Original Element | MVP Status | Notes |
|------------------|------------|-------|
| Multi-patient table | ❌ Remove | Single patient context |
| Issue/Status columns | ✅ Keep | What's missing |
| Urgent tag | ❌ Remove | No comparative urgency (single patient) |
| Resolve button | ✅ Keep | "Update with New Data" |

#### Completed/Exported → Submission Confirmation

| Original Element | MVP Status | Notes |
|------------------|------------|-------|
| Historical list | ⚠️ Modify | This patient's history only |
| "Synced to EMR" status | ✅ Keep | DocumentReference written |
| Auth number | ⚠️ Defer | Requires payer response tracking |

### MVP User Flow

1. **Provider opens patient chart** in athenaOne
2. **Provider clicks AuthScript** in Apps Tab
3. **SMART launch** provides patient context automatically
4. **Dashboard shows** this patient's PA status:
   - `PENDING` → "Analyzing encounter..."
   - `READY_FOR_REVIEW` → Form ready for approval
   - `MISSING_DATA` → Shows what's needed
   - `SUBMITTED` → "PDF written to chart"
5. **Provider approves** → PDF appears in patient chart
6. **Provider faxes** from athenaOne (manual step)

### Future Roadmap

When premium API tier becomes available:

1. **Phase 1:** Add ADT feed / FHIR Subscription integration
2. **Phase 2:** Build practice-wide patient registry with worklist
3. **Phase 3:** Enable multi-patient queue view
4. **Phase 4:** Add revenue metrics from payer contract data

---

*This reconciliation ensures the MVP is achievable within technical constraints while preserving the vision for future expansion.*