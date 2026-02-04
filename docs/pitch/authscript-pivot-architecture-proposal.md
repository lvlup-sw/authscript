# New AuthScript Architecture Proposal

## Executive Summary

The system follows a **Microservices Architecture** centered around a .NET Orchestrator. It interfaces with athenahealth via a generic FHIR facade and delegates cognitive tasks to a specialized Python AI service.

- **Primary Interface:** SMART on FHIR Web Application (Embedded in Athena).
- **Data Ingestion:** High-Frequency Polling of Certified FHIR Endpoints.
- **Intelligence:** Python/FastAPI service wrapping Azure OpenAI (GPT-4o).
- **Persistence:** Ephemeral (stateless) processing with metadata-only storage in PostgreSQL.

## End-to-End Data Flow

#### Phase 1: Detection (The Poller)

1. **Trigger:** Provider signs an encounter in athenahealth.
2. **Polling:** The **.NET Orchestrator** polls `GET /fhir/r4/Encounter` every 3-5 seconds, filtering by `status=finished` and `date=gt{last_check}`.
3. **Filtration:** System discards encounters irrelevant to the configured scope (e.g., checks against a "Target Payer List").
4. **Signal:** If valid, the Orchestrator emits an internal event to the Processing Queue.

#### Phase 2: Processing (The Intelligence Service)

1. **Hydration:** The Orchestrator queries athenahealth for the full patient context using Certified APIs:
   - `GET /Patient/{id}`
   - `GET /Condition` (Problem List)
   - `GET /MedicationStatement`
   - `GET /Observation` (Labs)
   - `GET /DocumentReference` (Clinical Notes)
2. **Normalization:** Data is mapped to standard FHIR R4 resources and bundled.
3. **Inference:** The bundle is transmitted to the **Python Intelligence Service** via HTTP.
   - **Extraction:** LLM extracts clinical rationale, previous treatments, and failure dates from unstructured notes.
   - **Determination:** Agent validates against hardcoded payer policy (e.g., "Must have failed 6 weeks of PT").
   - **Generation:** Service populates the specific PDF form and returns the binary stream to the Orchestrator.

#### Phase 3: Delivery (The UI)

1. **Notification:** The Orchestrator pushes a notification to the **Frontend** via Server-Sent Events (SSE).
2. **Presentation:** Provider clicks the AuthScript tab in the Patient Chart. The app launches (SMART on FHIR), validates the `patient_id` context, and renders the "Ready for Review" state.
3. **Submission:** Provider reviews the form. On confirmation, the Orchestrator executes a write-back:
   - `POST /fhir/r4/DocumentReference`: Uploads the PDF as a binary attachment linked to the Patient record.

------

### 3. Component Specifications

#### A. .NET Core Orchestrator (Backend)

- **Role:** Central logic, API Gateway, and State Manager.
- **Framework:** ASP.NET Core 10.
- **Key Services:**
  - `AthenaPollingService`: `IHostedService` implementing the `GET /Encounter` loop. Implements "Practice Profiling" to separate polling strategies per tenant.
  - `FhirClientFactory`: Manages OAuth2 client credentials and token rotation for athenahealth.
  - `SseHub`: Manages persistent `EventSource` connections for frontend real-time updates.
- **Compliance:** Performs no long-term PHI storage. PHI is held in memory during the transaction lifecycle only.

#### B. Python Intelligence Service (AI Engine)

- **Role:** Reasoning and Document Generation.
- **Framework:** FastAPI.
- **Model:** GPT-4o (via Azure OpenAI).
- **Pipeline:**
  - **Input:** FHIR Bundle (JSON).
  - **Logic:** Multi-stage chain (Selector -> Extractor -> Validator -> Filler).
  - **Output:** PDF Binary (PyMuPDF).
- **Isolation:** Runs inside a private VNET; accepts traffic only from the Orchestrator.

#### C. Embedded Frontend (UI)

- **Role:** Provider interaction surface.
- **Framework:** React + Vite.
- **Integration Model:** SMART on FHIR (Launch Sequence).
- **Authentication:** Validates the `launch` token provided by athenahealth during initialization to establish context (`user`, `patient`, `encounter`).
- **Visual Style:** Uses Athena-compliant CSS/design tokens to appear native.

------

### 4. Data Model & Storage

#### PostgreSQL (Metadata Store)

Strictly limits storage to operational metadata to minimize HIPAA liability.

- **Tenants Table:** `Id`, `PracticeId`, `ConfigProfile` (Polling rate, Enabled Payers).
- **Transactions Table:** `TransactionId`, `PatientId` (Hashed/Reference), `EncounterId`, `Status` (Processing, Ready, Submitted), `Timestamp`.

#### Redis (Hot Cache)

- Used for deduplication of poll results (preventing double-processing of the same encounter).
- Stores ephemeral "Draft" state if the user navigates away before submitting. TTL (Time-To-Live) set to 2 hours.

### 5. Integration Constraints (Certified APIs)

- **Read Access:** `System/Encounter.read`, `System/Patient.read`, `System/Observation.read`.
- **Write Access:** `System/DocumentReference.write` (Used to push the final PDF back to the chart).
- **Polling Rate:** Constrained to <1 request/sec per tenant to stay safely within the 15 req/sec (Preview) or 150 req/sec (Prod) limits.

-----

### References

- https://docs.athenahealth.com/api/guides/onboarding-overview
- https://docs.athenahealth.com/api/guides/certified-apis
- https://docs.athenahealth.com/api/guides/embedded-apps
- https://docs.athenahealth.com/api/guides/authorization-overview#Available_Scopes_for_3Legged_OAuth_Apps_5