# Strategic Pivot Memorandum: AuthScript

## Executive Summary

We are pivoting AuthScript from an **Epic/CDS Hooks** enterprise play to an **athenahealth/SMART on FHIR** solution targeting independent family practices.

This decision is driven by the incompatibility of the Epic ecosystem with our 6-week timeline and new customer discovery data indicating that independent clinics are the most viable entry point for the market.

Our new objective is to deploy a **Direct Integration Pilot** using athenahealth’s "Private App" architecture, delivering a production-ready, HIPAA-compliant tool that integrates into the provider's existing workflow without requiring a commercial marketplace contract.

------

## The Strategic Pivot

We are shifting our target market, platform, and technical architecture simultaneously to align with the reality of a rapid deployment.

| **Feature**         | **Previous Strategy**              | **New Strategy**                   |
| ------------------- | ---------------------------------- | ---------------------------------- |
| **Target Customer** | Large Health Systems (Enterprise)  | Independent Family Practices (SMB) |
| **Platform**        | Epic Systems                       | athenahealth                       |
| **Integration**     | CDS Hooks (Real-time Interruption) | SMART on FHIR (Embedded Dashboard) |
| **Trigger**         | Order Entry (Interruptive)         | Encounter Sign-Off (Passive/Async) |
| **Go-to-Market**    | App Orchard (6+ mo timeline)       | Private App (2-week timeline)      |

------

## Why We Are Pivoting

### A. Velocity & Access

- **Epic:** Accessing Epic’s production environment requires expensive sponsorships and long compliance cycles (App Orchard). It is impossible to achieve in 6 weeks.
- **athenahealth:** Offers a "Private App" model where a single practice can authorize our specific Client ID to access their data immediately. This allows us to bypass the commercial "Marketplace" review process for our initial pilot.

### B. Workflow Integration

- **Feedback:** Family physicians act as their own administrative staff. They cannot afford to leave the EMR to visit a standalone "AuthScript Portal."
- **Pivot:** We are moving to an **Embedded App** model. AuthScript will live *inside* the patient chart as a tab, utilizing SMART on FHIR so the doctor never manages a separate login or password.

### C. Technical Viability

- **Constraint:** We are restricted to [Certified APIs](https://docs.athenahealth.com/api/guides/certified-apis) to avoid prohibitive platform fees.
- **Solution:** We will architect a **High-Frequency Polling** mechanism that mimics real-time webhooks using standard Certified endpoints (`GET /Encounter`). This allows us to deliver a "real-time" experience without the contractual overhead of premium-tier event subscriptions.

------

## New Product Vision

The product is no longer a tool the doctor "uses" proactively. It is a background service that "cleans up" after them.

**The Workflow:**

1. **Draft:** Doctor treats the patient and signs the encounter note in athenahealth.
2. **Detect:** AuthScript detects the signature instantly (via Polling).
3. **Process:** Our AI reads the unstructured notes, maps the medical necessity to payer rules, and fills the PDF.
4. **Deliver:** A "Draft PA Request" notification appears in the patient's chart sidebar.
5. **Review:** Doctor clicks one button to review and confirms. The PDF is written back to the chart.

------

## Strategic Realities & Risks

### Reality 1: Pilot Environment Access (Critical Dependency)

**Risk:** We cannot access athenahealth's frontend Preview environment on our own. A current athenahealth customer must grant us user access to test and build the embedded UI.

**Mitigation:**

- Our pilot practice discussion is ongoing
- Fallback: Build and demo against sandbox APIs only (no embedded UI), show mockups for the embedded experience
- This is our **most critical dependency** — actively pursuing confirmation

### Reality 2: HIPAA Liability & Data Minimization

**Risk:** We are handling live Patient Health Information (PHI).

**Mitigation:** We are adopting a **"Pass-Through" Architecture**.

- We will **not** store patient records in our database.
- Data is fetched, processed in memory, sent to Athena, and immediately wiped.
- Our database stores only *metadata* (Transaction IDs, Status, Timestamps). If we are breached, the attacker gets a list of random numbers, not medical records.

### Reality 3: Scalability vs. Demo

**Risk:** The polling architecture cannot scale to 1,000 clinics on the Free Tier.

**Mitigation:** We are building for **Multi-Tenancy**.

- **Demo/Pilot Mode:** Aggressive polling (every 3-5s) for the pilot client.
- **Production Mode:** Increase polling interval to 10-15s, leverage batch hydration with concurrent limits.

**Validated Constraints:**
- Preview: 15 req/sec, 50,000 calls/day
- Production: 150 req/sec
- Our usage per 5s polling cycle: ~6 requests (1 poll + 5 hydration)
- Daily budget of 50K calls provides ample headroom for pilot

### Reality 4: We Are Not Competing with athenaPayer

**Risk:** athenahealth has a product called [athenaPayer](https://www.athenahealth.com/resources/blog/athenapayer-modernizing-prior-authorization).

**Mitigation:** athenaPayer is sold to *insurance companies* to ingest data. We are selling to *providers* to defend against denials. We are the "Defense Counsel" to athenaPayer's "Prosecutor."