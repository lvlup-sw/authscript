### System Architecture (Mermaid)

This diagram represents the **Local-First** architecture orchestrated by .NET Aspire.

Code snippet

```
graph TD
    subgraph "Epic Simulation"
        Epic[Epic EHR (Sandbox)]
        User[Doctor]
    end

    subgraph "Valkyrie System (.NET Aspire Host)"
        
        subgraph "Gateway Service (.NET 8)"
            CDSHook[CDS Hook Controller]
            Auth[OBO Token Handler]
            FHIRClient[FHIR Client]
            PDFStamper[PDF Form Filler]
        end

        subgraph "Intelligence Service (Python)"
            FastAPI[FastAPI Endpoint]
            Parser[PDF Ingestion (LlamaParse)]
            Reasoning[LLM Agent (GPT-4o)]
        end

        subgraph "Data & State"
            DB[(PostgreSQL)]
            LocalFiles[Blank PDF Templates]
        end
    end

    %% Workflow
    User -- 1. Signs Order --> Epic
    Epic -- 2. POST /cds-services (JWT) --> CDSHook
    CDSHook -- 3. Exchange Token (OBO) --> Auth
    
    Auth -- 4. Valid Token --> FHIRClient
    FHIRClient -- 5. GET /DocumentReference (Raw PDFs) --> Epic
    
    FHIRClient -- 6. Stream Raw PDF Bytes --> FastAPI
    FastAPI -- 7. Parse & Extract --> Parser
    Parser -- 8. Text Chunks --> Reasoning
    Reasoning -- 9. JSON Form Data --> CDSHook
    
    CDSHook -- 10. Load Template --> LocalFiles
    CDSHook -- 11. Map JSON to Fields --> PDFStamper
    PDFStamper -- 12. Upload Completed PDF --> Epic
    CDSHook -- 13. Return 'Success' Card --> User
```

------

### Component & Task Breakdown

Here is the engineering backlog, separated by architectural component.

#### 1. The Gateway Service (.NET 8 / C#)

*The "Coordinator." Handles the connection to Epic, security, and the final assembly of the PDF.*

- **Task 1.1: OBO Authentication Middleware**
  - Implement `Microsoft.Identity.Web` to validate the incoming Epic JWT.
  - Build the handshake logic to exchange the Epic User Token for a Graph/FHIR Access Token.
  - *Success Criteria:* A unit test that takes a dummy JWT and returns a valid Access Token.
- **Task 1.2: CDS Hook Listener (`OrderSignController`)**
  - Create the API endpoint that listens for the `order-sign` event.
  - Implement "Prefetch" logic: Deserialize the `ServiceRequest` (the order) to check if it matches "MRI Lumbar Spine."
  - If no match, return HTTP 200 (No-Op) immediately.
- **Task 1.3: The "Dumb Pipe" Client (HttpClient)**
  - Implement the `IntelligenceClient` class using `IHttpClientFactory`.
  - Logic: Accept `byte[]` from Epic, wrap it in `MultipartFormDataContent`, and stream it to the Python container.
  - *Constraint:* Do not attempt to read the text in C#. Just move the bytes.
- **Task 1.4: PDF Form Stamping Engine**
  - Import a library like `iText7` or `PdfSharp`.
  - Create a mapping logic: `TargetPDF.Field["PatientName"] = JSON.PatientName`.
  - Load the `assets/mri_auth_template.pdf` from the local file system.
  - Flatten the form (make it un-editable) after filling.
- **Task 1.5: The Write-Back (Epic Upload)**
  - Implement the FHIR `POST /DocumentReference` call.
  - Upload the *filled* binary PDF back to Epic, linking it to the specific `PatientID` and `EncounterID`.

#### 2. The Intelligence Service (Python / FastAPI)

*The "Brain." Handles messy inputs and produces clean JSON.*

- **Task 2.1: Ingestion API Endpoint**
  - Scaffold a FastAPI app with a single `POST /analyze` endpoint.
  - Accept `UploadFile` (the raw PDF stream).
- **Task 2.2: PDF Parsing (The "Eyes")**
  - Implement **LlamaParse** (recommended) or `Unstructured.io`.
  - Configuration: Set it to "Medical Mode" (if available) or optimize for tables/layouts.
  - Output: Clean Markdown text, preserving headers like "History of Present Illness."
- **Task 2.3: Reasoning Chain (The "Brain")**
  - Implement a LangChain runnable.
  - **Prompt Engineering:** Create a prompt that accepts the specific constraints of the "MRI Lumbar" policy (e.g., "Must have 6 weeks PT", "Must have attempted NSAIDs").
  - **Structured Output:** Force the LLM to return a strict JSON schema (using Pydantic) matching the form fields C# needs.
- **Task 2.4: Vector Storage (The "Memory")**
  - Implement the `pgvector` store connection.
  - Logic: Chunk the parsed text and save embeddings to Postgres. This allows the LLM to cite its sources ("Found PT evidence in document A, page 3").

#### 3. Database (PostgreSQL)

*The "Journal." Stores the proof of work.*

- **Task 3.1: Schema Design**
  - `RequestLog`: Stores `TransactionID`, `Timestamp`, `PatientHash` (anonymized), `ProcessingTimeMs`.
  - `DecisionAudit`: Stores the raw LLM `InputPrompt` and `OutputJSON` for debugging.
- **Task 3.2: Vector Extension**
  - Enable `pgvector` extension.
  - Create the `DocumentChunks` table with a vector column (1536 dimensions for Ada-002 or 3072 for 3-Large).

------

### UX Design Resource Allocation

Since the backend is invisible, the Designers are critical for **making the invisible visible** during the demo.

- **Task 4.1: The "Shadow Dashboard" (Web UI)**
  - *Problem:* The audience can't see C# code running.
  - *Solution:* Build a simple **Blazor Dashboard** (part of the .NET Aspire host).
  - *Features:*
    - **Live Feed:** Show "New Request Detected" -> "Parsing PDF..." -> "Found Evidence: 'Back Pain'" -> "Form Filled."
    - **Visual Diff:** Show the "Blank Form" vs. "Filled Form" side-by-side.
  - *Tech:* This connects to the same Postgres DB to poll for status updates.
- **Task 4.2: The Epic "Card" Design**
  - *Constraint:* Epic CDS Cards are JSON-defined and very rigid.
  - *Work:* Design the JSON payload to maximize clarity.
    - *Headline:* "Prior Auth Automation"
    - *Body:* "High confidence approval detected. Form 12345 has been drafted."
    - *Action Button:* "Review & Sign" (Links to the document).
- **Task 4.3: The Presentation Assets**
  - Create the "Blank PDF Template" that looks like a real insurance form (Blue Cross / United Healthcare branding) but simplified for the demo.

### Summary Checklist for Launch

1. **Repo Structure:** Monorepo with `/src/Gateway`, `/src/Intelligence`, `/src/AppHost`.
2. **Dev Environment:** Everyone installs **Docker Desktop** and **.NET 8 SDK**.
3. **Data:** Use `Synthea` to generate 5 "perfect" patient scenarios (PDFs) that guaranteed pass the logic, so the demo never fails.