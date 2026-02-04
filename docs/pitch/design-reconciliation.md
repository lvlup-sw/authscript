# Design Update: MVP Architecture Reconciliation

**Date:** January 31, 2026

**Status:** Approved for Implementation

**Context:** Reconciliation of Product Team Workflow (Miro) with Technical Constraints (athenahealth Certified API).

## 1. Architectural Decisions

### A. Polling Strategy (Replacing ADT Feeds)

- **Change:** The system will not use ADT feeds for discharge notifications as originally proposed by Product.
- **Implementation:** The **Gateway Polling Service** polls for `Encounter?status=finished`. When a finished encounter is detected, ServiceRequest resources are fetched during the hydration phase to identify orders requiring prior authorization.
- **Rationale:** ADT feeds require premium integration (FHIR Subscriptions are alpha-only). Polling Encounter.finished ensures clinical documentation is complete before PA analysis. ServiceRequest data identifies *what* treatment needs PA; Encounter context provides *why* (clinical justification).

### B. Submission Workflow (Write-Back Only)

- **Change:** The "Submission" phase is formally defined as **writing the PDF to the patient's chart**.
- **Constraint:** Automated faxing/electronic submission to payers is **Out of Scope** for MVP due to clearinghouse contract requirements.
- **New Flow:** App generates PDF $\rightarrow$ App uploads to athenahealth $\rightarrow$ User manually faxes from athenahealth UI.

## 2. Component Modifications

### Gateway Service (`.NET 10`)

#### 1. Updated `AthenaPollingService`

The polling logic triggers on finished encounters; ServiceRequest (orders) are fetched during hydration to identify treatments requiring PA.

- **Polling:** `GET /Encounter?status=finished` (unchanged)
- **Hydration (new):** Include `GET /ServiceRequest?encounter={id}` in the clinical data bundle
- **AI Filter Logic:**
  1. Extract CPT codes from ServiceRequest resources
  2. Check each CPT against payer's "requires PA" list
  3. Create work item for each qualifying order
- *Rationale:* Encounter.finished ensures clinical notes are complete; ServiceRequest identifies the specific treatment needing authorization.

#### 2. Authentication Strategy

- **Configuration:**
  - **App Type:** Web (Confidential Client).
  - **Auth Method:** Secret (Client Credentials).
  - **Redirect URI:** `http://localhost:3000/callback` (for local dev).

### Dashboard Service (`SMART on FHIR`)

#### 3. New Feature: "Manual Fax Support" (Optional)

To bridge the gap between "Write-Back" and "Submission," the UI must facilitate the manual workflow.

- **Add "Copy Payer Fax" Action:**
  - Display the specific Fax Number for the Payer (retrieved from Policy DB).
  - *User Story:* "As a coordinator, I can copy the correct fax number from the dashboard so I can paste it into the athenaFax window."
- **Add "Open in Chart" Deep Link:**
  - Direct link to the specific `DocumentReference` ID in athenaNet to save the user clicks.

### Intelligence Service (`Python`)

#### 4. "Missing Information" Handling

- **Change:** The "Loop" for missing data cannot rely on push notifications.
- **Implementation:**
  - The "Update with New Data" button in the UI triggers a **Force Re-Hydrate** cycle:

  ```
  UI: User clicks "Update with New Data"
    └─► Gateway: GET /DocumentReference (fetch new notes since last check)
        └─► Gateway: GET /Observation (fetch new labs/vitals)
            └─► Intelligence: Re-run LLM analysis with updated bundle
                └─► Gateway: Update work item status
                    └─► UI: Refresh work item (may transition to READY_FOR_REVIEW)
  ```

- **User Actions (from Product Workflow Path B):**
  - **"Update with New Data"** — Triggers re-hydration cycle above
  - **"Payer Requirements Not Met"** — Marks work item as `PAYER_REQUIREMENTS_NOT_MET`, optionally sends notification to ordering provider (future: write note to chart)

## 3. Revised Data Flow (MVP)

| **Step** | **Action**     | **Actor** | **Technical Implementation**                                 |
| -------- | -------------- | --------- | ------------------------------------------------------------ |
| **1**    | **Detect**     | System    | Poll `GET /Encounter?status=finished`.                       |
| **2**    | **Hydrate**    | System    | Fetch Patient, Condition, Observation, DocumentReference, **ServiceRequest**. |
| **3**    | **Filter**     | AI Agent  | Identify ServiceRequests with CPT codes requiring PA per payer policy. |
| **4**    | **Analyze**    | AI Agent  | LLM extracts clinical facts → evaluates against policy criteria. |
| **5**    | **Review**     | User      | Dashboard displays work item. User reviews, edits if needed. |
| **6**    | **Write-Back** | System    | `POST /DocumentReference` (PDF Binary). Class: `ClinicalDocument`. |
| **7**    | **Submit**     | **User**  | User opens Document in athenaNet → Clicks "Fax" → Pastes Number. |

### Work Item States

| **State** | **Trigger** | **UI Visibility** |
| --------- | ----------- | ----------------- |
| `READY_FOR_REVIEW` | All required fields populated | Yes - "Ready for Review" queue |
| `MISSING_DATA` | Required fields incomplete | Yes - "Missing Data" queue |
| `PAYER_REQUIREMENTS_NOT_MET` | User marks as unsubmittable | Yes - "Closed" with reason |
| `SUBMITTED` | User approves, PDF written to chart | Yes - "Submitted" queue |
| `NO_PA_REQUIRED` | AI determines no PA needed for CPT/payer | No - auto-closed |

## 4. Open Questions Resolved

- **Submission:** Confirmed as **Manual Fax** via athenaNet UI.
- **Notifications:** Confirmed as **In-App Badges** (Dashboard) rather than EHR Inbox Messages.
- **Trigger:** Confirmed as **Polling Encounter.finished** (3-5s interval) rather than ADT Push.
- **Intent to Treat Detection:** ServiceRequest resources fetched during hydration; AI filters for PA-requiring CPT codes.
- **Missing Data Loop:** User-initiated re-hydration via "Update with New Data" button.
- **Terminal States:** Added `PAYER_REQUIREMENTS_NOT_MET` for cases where required data cannot be provided.

## 5. Alignment with Product Workflow

| **Product Phase** | **Technical Implementation** |
| ----------------- | ---------------------------- |
| Phase 1: Intake | Encounter polling → ServiceRequest hydration → AI filter |
| Phase 2: Drafting | LangGraph pipeline: Policy retrieval → Fact extraction → Criteria evaluation → Form generation |
| Phase 3A: Ready for Review | Work item state `READY_FOR_REVIEW` → User approves → Write-back |
| Phase 3B: Missing Data | Work item state `MISSING_DATA` → User clicks "Update" → Re-hydrate → Re-analyze |
| Phase 4: Submission | PDF in chart → User manually faxes from athenaNet |