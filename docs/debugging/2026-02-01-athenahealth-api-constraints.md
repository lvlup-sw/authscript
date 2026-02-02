# athenahealth FHIR API Constraints Discovery

**Date:** 2026-02-01
**Status:** Blocking - Requires Architecture Decision

## Summary

During integration testing with athenahealth's FHIR R4 Certified API, we discovered fundamental constraints that break the polling-based architecture described in [athenahealth Pivot MVP](../designs/2026-01-29-athenahealth-pivot-mvp.md).

**Key Finding:** athenahealth does not support global/practice-wide FHIR searches. Every resource query requires patient-specific identifiers, making "watch for new encounters" polling impossible.

---

## Issues Discovered (Chronological)

### 1. OAuth Token 401 - Policy Evaluation Failed

**Symptom:**
```
Token request failed with status 401: {"detailedmessage":"Policy evaluation failed for this request, please check the policy configurations.","error":"access_denied"}
```

**Root Cause:** Multiple issues compounded:

1. **Missing `scope` parameter** - The original `AthenaTokenStrategy` wasn't sending scopes in the token request. athenahealth requires explicit scopes (no defaults).

2. **Wrong OAuth client** - AppHost secrets had a different `client_id` than Gateway.API secrets. When running via Aspire, the AppHost credentials were used.

**Fix:**
- Added `Scopes` property to `AthenaOptions` with all SMART v2 system scopes
- Updated `AthenaTokenStrategy` to include scopes in token request
- Synchronized credentials between AppHost and Gateway.API user-secrets

**Commits:**
- `df4ee27` - fix: send OAuth scopes in athenahealth token request

---

### 2. FHIR Request 404 - Missing /r4/ Path Segment

**Symptom:**
```
GET https://api.preview.platform.athenahealth.com/fhir/Encounter?* → 404
```

**Root Cause:** `FhirBaseUrl` was configured without trailing slash:
```
Base: https://api.preview.platform.athenahealth.com/fhir/r4
Request: Encounter?...
Result: https://api.preview.platform.athenahealth.com/fhir/Encounter?... ❌
```

When `HttpClient.BaseAddress` lacks a trailing slash, relative URI resolution replaces the last path segment instead of appending.

**Fix:**
```csharp
// Before
"FhirBaseUrl": "https://api.preview.platform.athenahealth.com/fhir/r4"

// After
"FhirBaseUrl": "https://api.preview.platform.athenahealth.com/fhir/r4/"
```

---

### 3. FHIR Request 400 - Missing ah-practice Parameter

**Symptom:**
```json
{"details":"Could not determine what practice the request was for. Add the `ah-practice` search parameter to your request."}
```

**Root Cause:** athenahealth is multi-tenant. Every FHIR request must include the practice identifier.

**Format:** `ah-practice=Organization/a-1.Practice-{PracticeId}`

**Fix:** Updated `AthenaPollingService` to include practice ID in queries.

---

### 4. FHIR Request 400 - Patient/ID Required (BLOCKING)

**Symptom:**
```json
{
  "issue": [{
    "details": {"text": "The search query could not be performed because one of the required parameter combinations [[patient],[_id]] was not provided."},
    "severity": "fatal",
    "code": "forbidden"
  }]
}
```

**Root Cause:** athenahealth's FHIR API does not support global searches. This affects ALL resources:

| Resource | Required Parameters |
|----------|-------------------|
| Encounter | `patient` OR `_id` |
| ServiceRequest | `patient` OR `_id` |
| Patient | `_id` OR `identifier` OR `name` OR `family+birthdate` OR `family+gender` OR `family+given` |
| Condition | `patient` (assumed) |
| Observation | `patient` (assumed) |

**This is a fundamental API constraint, not a configuration issue.**

---

## Impact on Architecture

### Original Design Assumption (Invalid)

From [athenahealth-pivot-mvp.md](../designs/2026-01-29-athenahealth-pivot-mvp.md):

```
Poll Encounter?status=finished
  └─► Queue encounter ID
      └─► EncounterProcessor hydrates clinical bundle
```

This assumed we could discover new encounters via:
```
GET /Encounter?status=finished&date=gt{lastCheck}
```

**This query is not supported by athenahealth.**

### What Works vs. What Doesn't

| Operation | Works? | Notes |
|-----------|--------|-------|
| Token acquisition (client_credentials) | ✅ | With correct scopes |
| Get specific patient by ID | ✅ | `GET /Patient/{id}` |
| Get patient's encounters | ✅ | `GET /Encounter?patient={id}` |
| Get patient's conditions | ✅ | `GET /Condition?patient={id}` |
| **Discover new encounters globally** | ❌ | Requires patient ID |
| **Discover new patients** | ❌ | Requires identifier/name |
| **Poll for practice-wide activity** | ❌ | No global queries |

---

## Architectural Options

### Option A: Manual Patient Enrollment

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Practice Staff │────▶│  Patient Registry │────▶│  Per-Patient    │
│  (manual entry) │     │  (PostgreSQL)     │     │  Polling        │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

**Flow:**
1. Staff enters patient IDs into AuthScript (e.g., from day's schedule)
2. System stores patient IDs in registry
3. Background service polls each registered patient for encounters/orders
4. When finished encounter detected → trigger PA workflow

**Pros:**
- Works within API constraints
- Simple to implement

**Cons:**
- Not "invisible" - requires manual step
- Doesn't match original product vision (ADT-triggered)

---

### Option B: Appointment Schedule Integration

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  athenahealth   │────▶│  Patient Registry │────▶│  Per-Patient    │
│  Schedule API   │     │  (extract patient │     │  FHIR Polling   │
│  (proprietary)  │     │   IDs from appts) │     │                 │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

**Flow:**
1. Nightly job fetches next day's appointments via athenahealth proprietary API
2. Extract patient IDs from appointments
3. Register patients for monitoring
4. Poll those patients via FHIR during/after their appointments

**Pros:**
- More automated than Option A
- Focuses on patients with scheduled visits

**Cons:**
- Requires additional API access (proprietary, not FHIR)
- Not real-time - batch-based
- May need separate auth/permissions

---

### Option C: Demo-Only Approach

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Hardcoded      │────▶│  Simulated       │────▶│  Real FHIR      │
│  Demo Patients  │     │  "Discovery"     │     │  Hydration      │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

**Flow:**
1. Pre-seed known sandbox patient IDs
2. Polling service "discovers" these patients (simulated)
3. Real FHIR hydration and PA workflow runs

**Pros:**
- Sufficient for CSE 589 demo
- Minimal architectural changes

**Cons:**
- Not production-viable
- Masks the real problem

---

### Option D: Webhook/Subscription (Not Available)

athenahealth does offer FHIR Subscriptions, but:
- Requires premium API tier (not Certified)
- Out of scope for current timeline/budget

---

## Recommendation

For **CSE 589 demo**: Use Option C (hardcoded demo patients) to demonstrate the PA workflow end-to-end.

For **production viability**: Option A (manual enrollment) is the most realistic near-term path. Option B could be explored if proprietary API access is available.

**The "invisible" vision from the original pitch requires either:**
1. athenahealth Subscription API access (premium tier)
2. ADT feed integration (hospital-level, not available for small practices)
3. Deep EHR integration (embedded app that captures patient context at point of care)

---

## Technical Fixes Applied

| File | Change |
|------|--------|
| `AthenaOptions.cs` | Added `Scopes` property with SMART v2 system scopes |
| `AthenaTokenStrategy.cs` | Include scopes in token request, add logging |
| `AppHost.cs` | Fixed `FhirBaseUrl` trailing slash |
| `appsettings.Development.json` | Fixed `FhirBaseUrl` trailing slash |
| `AthenaPollingService.cs` | Added `ah-practice` parameter to queries |
| AppHost user-secrets | Updated `athena-client-id` and `athena-client-secret` |

---

## References

- [athenahealth ah-practice Search Parameter](https://fhir.athena.io/athenacoreext/SearchParameter-ah-practice.html)
- [athenahealth FHIR Implementation Guide](https://sb.docs.mydata.athenahealth.com/fhir-r4/index.html)
- [SMART on FHIR v2 Scopes](https://hl7.org/fhir/smart-app-launch/scopes-and-launch-context.html)
- [aone-fhir-subscriptions (Premium API example)](https://github.com/athenahealth/aone-fhir-subscriptions)

---

## Next Steps

1. [ ] **Decision required:** Which architectural option to pursue?
2. [ ] Update design doc to reflect API constraints
3. [ ] If Option C: Create demo patient seed script
4. [ ] If Option A: Design patient enrollment UI/API
