# AuthScript MVP Design

## Executive Summary (2-Pager)

---

### The Problem

**Every week, doctors spend 13 hours fighting insurance paperwork instead of treating patients.**

Prior authorization (PA) is the insurance industry's requirement that providers obtain advance approval before delivering certain medical services. What began as a cost-control mechanism has metastasized into a $1.26 billion annual administrative burden that delays care, burns out clinicians, and paradoxically increases healthcare costs.

The numbers are stark:
- **94%** of physicians report care delays due to PA (AMA)
- **89%** cite PA as a significant contributor to burnout (AMA)
- **40%** of patients abandon prescribed therapies due to PA friction (Surescripts)
- **19%** of providers have seen PA delays cause serious adverse events (Surescripts)

The process is fundamentally broken: a physician who knows a treatment is medically necessary must spend 30+ minutes per request manually gathering clinical documentation, navigating payer-specific portals, and hoping the submission isn't denied for missing information. Multiply by 39 requests per week, and you have a workforce crisis masquerading as an administrative task.

---

### The Solution

**AuthScript is an AI-native clinical intelligence platform that automates prior authorization for complex medical procedures.**

Our system integrates directly into the physician's EHR workflow via CDS Hooks. When a doctor signs an order requiring authorization, AuthScript:

1. **Reads** the patient's complete medical record—notes, labs, imaging, treatment history
2. **Understands** what each insurance company requires by dynamically retrieving relevant policy guidelines
3. **Identifies gaps** before submission, prompting for missing documentation that would cause denial
4. **Generates** the complete authorization form with clinical justifications, diagnosis codes, and supporting evidence
5. **Uploads** the completed form back to the EHR, ready for physician review and submission

**Result:** What took 30 minutes now takes 3. Approval rates increase because the AI ensures submissions are "bulletproof" against medical necessity denials.

---

### Market Analysis & Competition

The PA market is served by three archetypes, each with significant gaps:

| Competitor | Model | Strength | Gap |
|------------|-------|----------|-----|
| **Surescripts** | Network Intermediary | Real-time pharmacy PA (22-second median approval) | Limited to medications; cannot handle complex medical procedures |
| **Waystar** | Enterprise RCM Platform | Comprehensive revenue cycle; 5,000+ payer connections | Relies on fragile RPA bots; enterprise pricing excludes small practices |
| **Promantra** | Service BPO | Handles complex cases with human expertise | Slow (days, not minutes); doesn't scale; limited visibility |

**The strategic gap:** None of these solutions effectively handle *unstructured clinical data* for complex medical procedures. Surescripts solved structured pharmacy data. Waystar automates form movement but not form content. Promantra uses humans to interpret clinical notes—slow and expensive.

**Market tailwind:** The CMS-0057-F mandate requires payers to adopt FHIR-based APIs by January 2026, with 72-hour urgent and 7-day standard decision timeframes. RPA-based solutions will struggle to meet these requirements. FHIR-native solutions have regulatory advantage.

---

### How AuthScript Differentiates

**1. Clinical Substantiation, Not Just Form Submission**

Competitors focus on moving forms faster. We focus on making forms *approvable*. Our Generative AI pre-checks clinical documentation against payer policies before submission, catching the deficiencies that cause 68% of denials.

**2. The "Messy Middle" of Medical Procedures**

We explicitly target high-complexity, high-cost procedures that Surescripts doesn't cover: orthopedic surgeries, advanced imaging, oncology regimens, cardiac interventions. These have the highest denial rates and financial impact.

**3. Policy-Agnostic Architecture**

Our extraction engine produces a general clinical profile. Adding new procedure types or payers means ingesting policy documents, not rewriting code. This scales where procedure-specific solutions cannot.

**4. Transparent, Citable Reasoning**

AuthScript shows its work: "Policy section 3.2 requires 6 weeks of conservative therapy; chart documents 8 weeks starting [date]." This builds physician trust and provides audit trails for compliance.

---

## Technical Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        AuthScript System (.NET Aspire Host)                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────┐      ┌─────────────────────────────────┐   │
│  │   Gateway Service (.NET 10) │      │  Intelligence Service (Python)  │   │
│  │                             │      │                                 │   │
│  │  ┌─────────────────────┐    │      │  ┌─────────────────────────┐    │   │
│  │  │ CDS Hook Controller │    │      │  │   General Extractor     │    │   │
│  │  │   (order-sign)      │    │ ───► │  │   (LlamaParse + LLM)    │    │   │
│  │  └─────────────────────┘    │      │  └───────────┬─────────────┘    │   │
│  │  ┌─────────────────────┐    │      │              │                  │   │
│  │  │   FHIR Client       │    │      │              ▼                  │   │
│  │  │ (DocumentReference, │    │      │  ┌─────────────────────────┐    │   │
│  │  │  Binary, Patient)   │    │      │  │   Clinical Profile      │    │   │
│  │  └─────────────────────┘    │      │  │   (Structured JSON)     │    │   │
│  │  ┌─────────────────────┐    │      │  └───────────┬─────────────┘    │   │
│  │  │   PDF Form Stamper  │    │      │              │                  │   │
│  │  │   (iText7)          │    │ ◄─── │              ▼                  │   │
│  │  └─────────────────────┘    │      │  ┌─────────────────────────┐    │   │
│  │  ┌─────────────────────┐    │      │  │   RAG Policy Lookup     │    │   │
│  │  │  Auth Handler       │    │      │  │   (pgvector + retrieval)│    │   │
│  │  │  (SMART Backend)    │    │      │  └───────────┬─────────────┘    │   │
│  │  └─────────────────────┘    │      │              │                  │   │
│  └─────────────────────────────┘      │              ▼                  │   │
│                                       │  ┌─────────────────────────┐    │   │ 
│                                       │  │   Reasoning Agent       │    │   │
│                                       │  │   (Gap Analysis + Fill) │    │   │
│                                       │  └─────────────────────────┘    │   │
│                                       └─────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                     PostgreSQL + pgvector                           │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────┐  │    │
│  │  │ RequestLog  │  │DecisionAudit│  │ PolicyChunks (vector store) │  │    │
│  │  └─────────────┘  └─────────────┘  └─────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Hybrid RAG Pipeline (Core Differentiator)

The Intelligence Service implements a two-stage pipeline:

**Stage 1: General Clinical Extraction**

```
Input: Raw clinical documents (PDFs, FHIR resources)
    │
    ▼
┌─────────────────────────────────────────────┐
│  LlamaParse (or Unstructured.io)            │
│  - PDF → Markdown with structure preserved  │
│  - Table extraction                         │
│  - Section header detection                 │
└─────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────┐
│  Extraction LLM (GPT-4o / Claude)           │
│  Prompt: "Extract all clinically relevant   │
│  facts into structured profile..."          │
│                                             │
│  Output Schema (Pydantic):                  │
│  - chief_complaints: List[Complaint]        │
│  - diagnoses: List[Diagnosis]               │
│  - treatments: List[Treatment]              │
│  - medications: List[Medication]            │
│  - imaging: List[ImagingStudy]              │
│  - lab_results: List[LabResult]             │
│  - conservative_therapy: List[Therapy]      │
│  - surgical_history: List[Surgery]          │
└─────────────────────────────────────────────┘
    │
    ▼
Output: Clinical Profile (procedure-agnostic JSON)
```

**Stage 2: Policy-Aware Reasoning**

```
Input: Clinical Profile + Requested Procedure + Payer ID
    │
    ▼
┌─────────────────────────────────────────────┐
│  RAG Retrieval (pgvector)                   │
│  Query: procedure + payer + "medical        │
│         necessity" + "prior authorization"  │
│  Returns: Top-k policy chunks with citations│
└─────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────┐
│  Reasoning LLM                              │
│  Inputs:                                    │
│  - Clinical Profile                         │
│  - Retrieved Policy Chunks                  │
│  - Target Form Schema                       │
│                                             │
│  Tasks:                                     │
│  1. Compare profile against policy reqs     │
│  2. Identify satisfied requirements         │
│  3. Flag gaps with specific citations       │
│  4. Generate form field mappings            │
│                                             │
│  Output Schema:                             │
│  - requirements_met: List[Requirement]      │
│  - gaps: List[Gap]                          │
│  - form_data: Dict[field_name, value]       │
│  - confidence_score: float                  │
│  - citations: List[Citation]                │
└─────────────────────────────────────────────┘
    │
    ▼
Output: Form Data + Gap Analysis + Evidence Trail
```

### Epic Integration Specifics

**CDS Hooks Flow:**

1. Physician signs order in Epic
2. Epic POSTs to `POST /cds-services/authscript-pa` with:
   - `hook`: "order-sign"
   - `context.draftOrders`: The ServiceRequest being signed
   - `fhirAuthorization`: Token for FHIR callbacks
   - `prefetch`: Pre-fetched Patient resource (if configured)

3. AuthScript Gateway:
   - Validates request
   - Uses `fhirAuthorization.access_token` to fetch DocumentReference resources
   - Retrieves Binary attachments (the actual PDFs)
   - Streams to Intelligence Service

4. Intelligence Service processes, returns form data

5. Gateway stamps PDF template, uploads via `POST /DocumentReference`

6. Returns CDS Card:
   ```json
   {
     "cards": [{
       "summary": "Prior Auth Form Generated",
       "detail": "High confidence approval. Form ready for review.",
       "indicator": "info",
       "source": { "label": "AuthScript" },
       "links": [{
         "label": "Review Form",
         "url": "https://epic.example.com/DocumentReference/123",
         "type": "absolute"
       }]
     }]
   }
   ```

**Authentication:**

Per CDS Hooks spec, Epic provides `fhirAuthorization` in the request context. For backend service calls (if needed), use SMART Backend Services:
- Register public key with Epic
- Generate signed JWT assertion
- Exchange for access token via client_credentials grant

### Available CDS Hooks Actions (Sandbox)

The following FHIR operations are available as CDS Hooks suggestion actions:

```
ServiceRequest.Create (Unsigned Order)
ServiceRequest.Delete (Unsigned Order)
ServiceRequest.Read (Unsigned Order)
ServiceRequest.Update (Unsigned Order)
MedicationRequest.Create (Unsigned Order)
MedicationRequest.Delete (Unsigned Order)
MedicationRequest.Read (Unsigned Order)
Condition.Create (Encounter Diagnosis)
Condition.Create (Problems)
```

**Key capability:** `ServiceRequest.Update` enables AuthScript to annotate the order with PA status:

```json
{
  "cards": [{
    "summary": "Prior Auth Form Generated",
    "indicator": "info",
    "source": { "label": "AuthScript" },
    "links": [{
      "label": "Review Form",
      "url": "{{documentReference.url}}",
      "type": "absolute"
    }]
  }],
  "systemActions": [{
    "type": "update",
    "resource": {
      "resourceType": "ServiceRequest",
      "id": "{{orderId}}",
      "extension": [{
        "url": "http://authscript.com/fhir/pa-status",
        "valueString": "form-generated"
      }]
    }
  }]
}
```

### Database Schema

```sql
-- Request tracking
CREATE TABLE request_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id VARCHAR(255) NOT NULL,
    timestamp TIMESTAMPTZ DEFAULT NOW(),
    patient_hash VARCHAR(64),  -- SHA-256 of MRN for privacy
    procedure_code VARCHAR(20),
    payer_id VARCHAR(50),
    processing_time_ms INTEGER,
    status VARCHAR(20),  -- pending, processing, completed, failed
    confidence_score DECIMAL(3,2)
);

-- Audit trail for compliance
CREATE TABLE decision_audit (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID REFERENCES request_log(id),
    input_documents JSONB,      -- metadata only, not PHI
    clinical_profile JSONB,
    policy_chunks_used JSONB,   -- which chunks were retrieved
    llm_prompt TEXT,
    llm_response JSONB,
    gaps_identified JSONB,
    form_output JSONB
);

-- Policy vector store
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE policy_chunks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payer_id VARCHAR(50) NOT NULL,
    procedure_category VARCHAR(100),
    document_name VARCHAR(255),
    section_header VARCHAR(255),
    chunk_text TEXT,
    embedding vector(1536),  -- OpenAI ada-002
    metadata JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX ON policy_chunks USING ivfflat (embedding vector_cosine_ops);
```

### Team Allocation (8-Week Plan)

| Week | C# Team (4) | Python Team (2) | DB (1) | UX (2) | MBA (2) |
|------|-------------|-----------------|--------|--------|---------|
| 1 | CDS Hook scaffold, Auth research | FastAPI scaffold, LlamaParse POC | Schema design | PA form templates (AcroForm) | Pitch deck v1 |
| 2 | FHIR Client (DocumentReference) | Extraction prompt engineering | pgvector setup | Form field mapping design | Market sizing |
| 3 | PDF retrieval flow, Mock Epic harness | Clinical profile schema | Policy ingestion scripts | CDS Card UX design | Competitor deep-dive |
| 4 | iText7 integration | RAG retrieval pipeline | Index optimization | Synthea test scenarios | Demo script draft |
| 5 | Form stamping logic | Reasoning agent prompts | Audit logging | Golden path validation | Investor outreach |
| 6 | Epic upload flow | Gap analysis output | Performance testing | End-to-end UX review | Pitch rehearsal |
| 7 | Integration testing | Edge case handling | Load testing | Demo golden paths | Demo rehearsal |
| 8 | Bug fixes, hardening | Prompt tuning | Monitoring setup | Final polish | Live demo |

### Risk Mitigation (Summary)

| Risk | Mitigation | Owner |
|------|------------|-------|
| CDS Hooks integration | ✅ Sandbox confirmed; build mock harness for local dev | C# Team |
| LLM extraction quality | Test suite of 20+ chart variations; confidence scoring; validation prompts | Python Team |
| Policy retrieval misses | Seed vector store with 5+ payers' policies; manual QA of chunk quality | Python + DB |
| Demo failure | Pre-cache 3 golden scenarios; graceful fallback to cached results | All |
| PDF form mapping | Use custom AcroForm templates (not real insurer XFA forms) | UX |
| Test data gaps | Generate with Synthea + chatty-notes for controlled scenarios | UX + Python |

See **Technical Feasibility Analysis** section for detailed risk assessment.

### Demo Scenario (Golden Path)

**Patient:** Sarah Johnson, 45, chronic lower back pain
**Procedure:** MRI Lumbar Spine
**Payer:** Blue Cross Blue Shield (simulated)

**Chart contains:**
- 8 weeks of documented physical therapy
- Failed trial of NSAIDs (ibuprofen, naproxen)
- Lumbar X-ray showing degenerative changes
- Pain severity 7/10, affecting daily activities

**Demo flow:**
1. Physician opens Sarah's chart in Epic (sandbox)
2. Orders "MRI Lumbar Spine without contrast"
3. Signs order → triggers CDS Hook
4. (Behind scenes: Gateway fetches documents, Intelligence Service extracts and reasons)
5. AuthScript returns CDS Card: "Prior Auth Form Ready - High Confidence"
   - Card detail shows: "Found 8 weeks PT documentation, NSAID trial, lumbar X-ray"
6. Physician clicks "Review Form" link → sees completed PDF with:
   - Patient demographics filled
   - Diagnosis codes mapped (M54.5 Low back pain)
   - Clinical justification citing PT duration, failed medications, imaging
   - All policy requirements marked as satisfied
7. Physician reviews, signs, and submits

**Talking points during demo:**
- "Notice we didn't ask the doctor to hunt for documentation"
- "The system found the PT notes from 2 months ago automatically"
- "It knows Blue Cross requires 6 weeks of conservative therapy—we documented 8"
- "What took 30 minutes now took 45 seconds"

---

## Success Criteria

**For class demo (Week 8):**
- [ ] End-to-end flow works for 1 procedure type (MRI Lumbar Spine)
- [ ] Real Epic sandbox integration via CDS Hooks
- [ ] Visible "intelligence"—CDS Card detail shows extracted evidence and reasoning
- [ ] Professional PA form template with correct field mapping
- [ ] Compelling 5-minute pitch + 5-minute live demo
- [ ] 3 golden path scenarios that reliably succeed

**For product viability:**
- [ ] Extraction works across 3+ clinical note formats
- [ ] Policy RAG retrieves relevant guidelines for 3+ payers
- [ ] Gap analysis catches at least 80% of denial-causing deficiencies in test set
- [ ] Processing time < 60 seconds end-to-end
- [ ] Audit trail captures all LLM inputs/outputs for compliance

---

## Technical Feasibility Analysis

This section provides a detailed assessment of each technical component, based on research conducted January 2025.

### Feasibility Summary

| Component | Status | Confidence | Key Finding |
|-----------|--------|------------|-------------|
| **CDS Hooks Integration** | ✅ Feasible | 90% | Sandbox supports order-sign + ServiceRequest operations |
| **PDF Extraction (LlamaParse)** | ✅ Feasible | 75% | Works well; needs validation layer for complex layouts |
| **Policy RAG (pgvector)** | ✅ Feasible | 95% | Production-ready for this workload scale |
| **.NET Aspire + Python** | ✅ Feasible | 95% | First-class Python support in Aspire 13 |
| **PDF Form Filling (iText7)** | ✅ Feasible | 90% | AcroForm supported; use custom templates for demo |
| **Epic FHIR Resources** | ✅ Feasible | 85% | DocumentReference/Binary available; supplement with Synthea |
| **8-Week Timeline** | ⚠️ Tight | 75% | Achievable with scope discipline |

**Overall Assessment: FEASIBLE** — No blocking technical risks identified.

---

### Component Analysis

#### 1. Epic CDS Hooks Integration

**Status:** ✅ FEASIBLE (90% confidence)

**Sandbox Capabilities Confirmed:**
- `order-sign` hook trigger available
- `ServiceRequest.Create/Read/Update/Delete` actions supported
- `fhirAuthorization` provides OAuth token for FHIR callbacks

**Authentication Flow (per Epic docs):**

| Token | Purpose | Location |
|-------|---------|----------|
| JWT in `Authorization: Bearer` header | Authenticates Epic to your service | Request header |
| `fhirAuthorization.access_token` | Your service calls Epic FHIR APIs | Request body |
| `jku` claim | JWK Set URL for signature validation | JWT header |

**Security requirement:** Validate `jku` against allowlist before accepting JWTs.

**Epic Extensions to Handle:**

| Extension | Purpose |
|-----------|---------|
| `com.epic.cdshooks.request.bpa-trigger-action` | Maps hook to Epic action (23 = Sign Orders) |
| `com.epic.cdshooks.request.cds-hooks-specification-version` | Spec version |
| `com.epic.cdshooks.request.fhir-version` | FHIR version for resources |

**References:**
- [Epic CDS Hooks Documentation](https://fhir.epic.com/Documentation?docId=cds-hooks)
- [HL7 CDS Hooks Specification](https://cds-hooks.hl7.org/1.0/)

---

#### 2. PDF Extraction (LlamaParse)

**Status:** ✅ FEASIBLE with caveats (75% confidence)

**Strengths:**
- ~99% accuracy claimed for simple text ([Procycons Benchmark](https://procycons.com/en/blogs/pdf-data-extraction-benchmark/))
- Fast processing (~6s consistently)
- Preserves table structures in most cases
- 15% accuracy improvement on financial documents vs alternatives

**Weaknesses:**
- Struggles with multi-column layouts and word merging
- Table data accuracy issues: "looks like tables but data needs cleaning"
- Hierarchical structure flattening (uses uniform `#` levels)
- Currency symbols and footnotes problematic

**Risk for Clinical Notes:**
Clinical notes often have multi-column layouts, tables (lab values), and complex formatting. LlamaParse may produce structurally correct but factually incorrect output.

**Mitigations:**
1. **Validation prompts:** LLM post-check to verify extracted facts against source
2. **Confidence scoring:** Flag low-confidence extractions for human review
3. **Test suite:** Build 20+ varied clinical note formats for regression testing
4. **Ensemble fallback:** Consider [Docling](https://github.com/DS4SD/docling) as backup parser

**References:**
- [LlamaParse Documentation](https://docs.llamaindex.ai/en/stable/llama_cloud/llama_parse/)
- [PDF Extraction Comparison](https://llms.reducto.ai/document-parser-comparison)

---

#### 3. Policy RAG (pgvector)

**Status:** ✅ FULLY FEASIBLE (95% confidence)

**Performance Characteristics:**
- Works well for <10M vectors with sub-100ms latency
- 1536 dimensions (OpenAI ada-002) feasible at expected scale
- HNSW index recommended for fast retrieval
- Can reduce to 768 dims with PCA for 2x throughput (97% accuracy retained)

**For AuthScript Use Case:**
- Policy documents = likely <100K chunks (small dataset)
- Query latency requirement: <500ms acceptable
- HNSW index will exceed requirements

**Production Tips:**
- Hybrid retrieval: Combine vector KNN with structured filters (payer_id, procedure_category)
- Two-stage pipeline: Fast ANN recall → exact distance re-rank
- Index with `ivfflat` for larger datasets, `hnsw` for speed

**References:**
- [pgvector GitHub](https://github.com/pgvector/pgvector)
- [pgvector Production Guide](https://medium.com/@1nick1patel1/postgres-pgvector-production-retrieval-on-a-budget-814df87df5c9)

---

#### 4. .NET Aspire + Python Integration

**Status:** ✅ FULLY FEASIBLE (95% confidence)

**Aspire 13 Features (Released Nov 2025):**
- First-class Python support via `AddPythonApp()` and `AddPythonProject()`
- Native FastAPI/Uvicorn integration with health checks
- Automatic virtual environment management from `requirements.txt`
- Service discovery between .NET and Python services
- Unified dashboard for all services

**Integration Pattern:**
```csharp
// AppHost/Program.cs
var intelligence = builder.AddPythonProject("intelligence", "../Intelligence")
    .WithHttpEndpoint(port: 8000, name: "http");

var gateway = builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(intelligence);
```

**References:**
- [Aspire Python Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-python)
- [Aspire 13 Announcement](https://visualstudiomagazine.com/articles/2025/11/12/microsoft-releases-aspire-13.aspx)

---

#### 5. PDF Form Filling (iText7)

**Status:** ✅ FEASIBLE for demo (90% confidence)

**Supported Features:**
- AcroForm field filling via `PdfAcroForm.GetAcroForm()`
- Form flattening (make un-editable) after filling
- Field enumeration and mapping

**Limitations:**
- **XFA forms require commercial add-on** (pdfXFA) — real insurer forms may use XFA
- `NeedAppearances` flag deprecated in PDF 2.0
- Appearance generation can be slow; disable for controlled templates

**Demo Strategy:**
1. Create custom AcroForm-based templates (Blue Cross style, not actual forms)
2. Control the template design to avoid XFA/complex features
3. Flatten after filling for clean output

**References:**
- [iText Form Filling](https://kb.itextpdf.com/itext/filling-out-forms)
- [iText Form Flattening](https://kb.itextpdf.com/home/it7kb/examples/flattening-a-form)

---

#### 6. Epic FHIR Resources (DocumentReference/Binary)

**Status:** ✅ FEASIBLE (85% confidence)

**Available in Sandbox:**
- `DocumentReference.Search` — find clinical documents for patient
- `DocumentReference.Read` — get document metadata
- `Binary.Read` — retrieve actual document content (PDF, CDA)

**Document Retrieval Flow:**
```
1. GET /DocumentReference?patient={{patientId}}&category=clinical-note
   → Returns list of documents with metadata

2. Extract: DocumentReference.content.attachment.url
   → Points to Binary resource

3. GET /Binary/{{binaryId}}
   → Returns raw document bytes (PDF, CDA XML)
```

**Test Data Limitation:**
Epic sandbox test patients may not have rich PA-relevant documentation (PT notes, medication trials).

**Solution: Synthea + Chatty-Notes**
```bash
# Generate patients with back pain scenario
java -jar synthea.jar -p 10 -m back_pain

# Generate clinical notes from FHIR bundles
python chatty-notes/chatty.py patient_bundle.json
```

This creates controlled, repeatable test scenarios with known clinical content.

**References:**
- [Epic DocumentReference Spec](https://fhir.epic.com/Specifications?api=1048)
- [Synthea](https://github.com/synthetichealth/synthea)
- [Chatty-Notes](https://github.com/synthetichealth/chatty-notes)

---

### Updated Risk Matrix

| Risk | Likelihood | Impact | Mitigation | Status |
|------|------------|--------|------------|--------|
| CDS Hooks sandbox unavailable | ~~High~~ **Low** | High | Sandbox confirmed available | ✅ Resolved |
| LLM extraction errors on complex layouts | Medium | Medium | Validation prompts; confidence scores; test suite | Mitigated |
| Test data lacks PA-relevant content | Medium | Medium | Generate with Synthea + chatty-notes | Mitigated |
| Real insurer PDFs use XFA | Low (demo) | Low | Use custom AcroForm templates | Mitigated |
| pgvector performance | Low | Low | Small dataset; HNSW index | Low risk |
| .NET/Python integration | Low | Low | Aspire 13 native support | Low risk |
| Demo failure during presentation | Medium | High | Pre-cache golden scenarios; graceful fallback | Mitigated |
| 8-week timeline slip | Medium | Medium | Scope discipline; cut shadow dashboard | Managed |

---

### Recommendations

#### Cut from Scope: Shadow Dashboard

The Blazor "Shadow Dashboard" was proposed to visualize processing during demo. Given timeline constraints:

- **Cut:** Building a separate Blazor dashboard
- **Keep:** Console logging visible on presenter's screen during demo
- **Alternative:** Simple status display in the CDS Card detail field

This saves ~1 engineer-week of effort.

#### Add to Scope: Mock Epic Test Harness

For local development without sandbox connectivity:

```
┌─────────────────────────┐
│   Mock Epic Sender      │  (Simple .NET console app)
│   - Sends order-sign    │
│   - Mocks fhirServer    │
│   - Returns test docs   │
└───────────┬─────────────┘
            │ POST /cds-services/authscript-pa
            ▼
┌─────────────────────────┐
│   AuthScript Gateway    │
└─────────────────────────┘
```

Effort: ~2-3 days. Enables offline development and CI testing.

#### Test Data Strategy

1. **Primary:** Use Synthea to generate 5-10 patient scenarios with controlled clinical content
2. **Supplement:** Use chatty-notes to generate realistic clinical notes from FHIR bundles
3. **Golden paths:** Create 3 "perfect" scenarios that always pass for demo reliability
4. **Edge cases:** Create 2-3 scenarios with gaps (missing PT, no medication trial) to show gap detection

---

## Appendix: Key Resources

### Epic & FHIR
- [Epic on FHIR](https://fhir.epic.com/) — Developer documentation and sandbox
- [Epic CDS Hooks Documentation](https://fhir.epic.com/Documentation?docId=cds-hooks) — Epic-specific CDS implementation
- [HL7 CDS Hooks Specification](https://cds-hooks.hl7.org/1.0/) — Hook definitions, prefetch, cards
- [CDS Hooks Sandbox](https://sandbox.cds-hooks.org/) — Public testing tool (patient-view only)
- [Open Epic](https://open.epic.com/) — Sandbox access and interoperability guide

### Regulatory
- [CMS-0057-F Rule](https://www.cms.gov/newsroom/fact-sheets/cms-interoperability-and-prior-authorization-final-rule-cms-0057-f) — Prior auth API requirements (Jan 2026)

### PDF & Document Processing
- [LlamaParse](https://docs.llamaindex.ai/en/stable/llama_cloud/llama_parse/) — AI-native PDF extraction
- [Docling](https://github.com/DS4SD/docling) — Alternative PDF parser (ensemble fallback)
- [iText7 Form Filling](https://kb.itextpdf.com/itext/filling-out-forms) — PDF AcroForm manipulation

### Data & Infrastructure
- [pgvector](https://github.com/pgvector/pgvector) — Vector similarity search for PostgreSQL
- [.NET Aspire Python Integration](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-python) — Orchestrating Python services

### Test Data Generation
- [Synthea](https://github.com/synthetichealth/synthea) — Synthetic patient generator
- [Chatty-Notes](https://github.com/synthetichealth/chatty-notes) — Clinical note generator from FHIR bundles
- [Synthea Downloads](https://synthea.mitre.org/downloads) — Pre-generated datasets
