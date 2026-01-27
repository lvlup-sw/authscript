# Meeting Agenda: AuthScript Engineering Kickoff

**Date:** [TBD]
**Duration:** 60 minutes
**Attendees:** Reed (Lead), Anshula, Jessica, [Engineering Team]
**Goal:** Align on product vision, Epic integration status, system architecture, and developer onboarding

---

## 1. Product Context & Vision (10 min)

**Lead:** Reed
**Goal:** Ensure Anshula and Jessica are aligned on what we're building

- **The Problem:** Physicians spend 13+ hours/week on prior authorization paperwork
- **Our Solution:** AI agent that reads EHR, matches payer policy, auto-fills PA forms
- **Demo Target:** March 11, 2026 VC pitch at Pioneer Square Labs
- **Scope for Demo:**
  - Single procedure: TBD
  - Single payer: TBD
  - "Bulletproof happy path" philosophy

**Discussion:** Any questions on product direction?

---

## 2. Epic EHR Integration Status (15 min)

**Lead:** Reed
**Goal:** Review access requirements and assign ownership

### Current Status

| Item | Status | Owner | Blocker |
|------|--------|-------|---------|
| FHIR sandbox account (fhir.epic.com) | Created | Reed | None |
| Hyperdrive test harness download | Not started | ? | Need account |
| Individual user accounts (MFA) | Not started | ? | 1 per developer |
| Client ID registration | Not started | ? | Need scope definition |
| CDS Hooks registration | Not started | ? | Need OAuth first |

### Access Requirements (from docs/access-requirements.md)

1. Organization FHIR sandbox account ✓
2. Download Hyperdrive test harness from open.epic.com
3. Request individual user accounts (MFA required, 48-hour passcode)
4. Create/request Client ID for FHIR APIs
5. Additional build coordination with Epic contact

### Action Items to Assign

- [ ] Who downloads Hyperdrive and tests installation?
- [ ] Who requests user accounts for team members?
- [ ] Who defines OAuth scopes for Client ID request?

---

## 3. High-Level System Design (20 min)

**Lead:** Reed
**Goal:** Walk through architecture and get team buy-in

### System Overview

```
Epic Hyperdrive → CDS Hook (ServiceRequest.C/R/U/D) → Gateway Service
                                                   ↓
                                           FHIR Data Aggregation
                                                   ↓
                                           Intelligence Service (LLM)
                                                   ↓
                                           PDF Form Stamping
                                                   ↓
                                           Upload to Epic + CDS Card Response
```

### Component Breakdown

| Component | Tech Stack | Responsibility | Current State |
|-----------|------------|----------------|---------------|
| **Gateway** | .NET 10 | CDS Hooks, FHIR queries, PDF stamping, orchestration | 40% implemented |
| **Intelligence** | Python/FastAPI | LLM reasoning, evidence extraction, form generation | 60% implemented |
| **Dashboard** | React 19 | Status visibility, SMART app fallback | 100% UI complete |
| **Orchestration** | .NET Aspire | Service discovery, PostgreSQL, Redis | 100% complete |

### Key Integration Points

1. **CDS Hooks** — Epic fires `ServiceRequest.C/R/U/D` when physician creates a ServiceRequest
2. **FHIR R4 API** — We fetch Conditions, Observations, Procedures, DocumentReferences
3. **OAuth 2.0** — JWT validation + access token for FHIR queries
4. **DocumentReference.write** — Upload completed PA form back to Epic

### Design Decisions to Validate

- Using `ServiceRequest.C/R/U/D` resource hook — triggers on ServiceRequest lifecycle events
- Pre-caching LLM responses for demo patients — sub-100ms response
- Fallback to SMART app if CDS Hook fails

**Discussion:** Questions on architecture? Concerns about any component?

---

## 4. Repository & Onboarding (15 min)

**Lead:** Reed
**Goal:** Get developers set up and productive

### Repository Structure

```
prior-auth/
├── apps/
│   ├── gateway/          # .NET 10 API (54 files)
│   ├── intelligence/     # Python FastAPI (17 files)
│   └── dashboard/        # React 19 (55 files)
├── orchestration/
│   └── AuthScript.AppHost/  # Aspire orchestration
├── shared/
│   ├── types/            # @authscript/types
│   └── validation/       # @authscript/validation (Zod)
├── docs/
│   ├── designs/          # Architecture docs
│   └── access-requirements.md
└── scripts/              # Build automation
```

### Prerequisites

- .NET 10 SDK
- Python 3.11+
- Node.js 20+
- Docker Desktop
- VS Code or Rider

### Getting Started

```bash
git clone <repo>
cd prior-auth
npm install           # Install JS dependencies
dotnet restore        # Restore .NET packages
npm run dev           # Start Aspire orchestrator
```

### Test Gap Warning

| Service | Files | Tests |
|---------|-------|-------|
| Gateway | 54 | 0 |
| Intelligence | 17 | 0 |
| Dashboard | 55 | 11 ✓ |

### Onboarding Tasks

- [ ] Clone repo and run `npm run dev`
- [ ] Review architecture doc: `docs/designs/2025-01-21-authscript-demo-architecture.md`
- [ ] Review Epic access doc: `docs/access-requirements.md`
- [ ] Request Epic user account from Reed

---

## Wrap-Up & Next Steps (5 min)

### Assignments to Confirm

| Area | Owner |
|------|-------|
| Epic OAuth + FHIR integration | ? |
| LLM reasoning chains | ? |
| PDF template acquisition | ? |
| Synthea patient generation | ? |
| Test coverage (Gateway) | ? |
| Test coverage (Intelligence) | ? |

**Next Meeting:** [TBD — weekly sync?]

### Parking Lot

(capture items for follow-up)
