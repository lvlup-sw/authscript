# Refactor: Replace MockDataService with PostgreSQL Persistence

**Date:** 2026-02-21
**Feature ID:** `refactor-remove-mockdata`
**Status:** Draft

## Problem Statement

`MockDataService` is a 427-line singleton that provides **all data** for the dashboard — patients, PA requests, reference data, stats, activity — entirely in-memory with hardcoded seed values. This means:

1. **Dashboard shows fabricated data** — 37 pre-seeded PA requests with random confidence scores and fake criteria
2. **Data is lost on restart** — all PA requests disappear when the Gateway restarts
3. **FHIR patient IDs use wrong format** — MockDataService stores `"60178"` but the Athena FHIR R4 API requires `"a-195900.E-60178"`, causing 400 errors on all FHIR calls
4. **ProcessPARequest works** (we just wired it in e2e-wiring) but the results are stored in-memory and lost on restart

## Architecture Context

### Current State
```
Dashboard → GraphQL → MockDataService (in-memory singleton)
                        ├── 37 fake PA requests (hardcoded)
                        ├── 9 procedures, 6 medications (hardcoded)
                        ├── 10 diagnoses, 5 payers, 3 providers (hardcoded)
                        └── CRUD + stats + activity (all in-memory)
```

### Target State
```
Dashboard → GraphQL → PARequestStore (PostgreSQL)
                        ├── PA requests (persisted, real analysis results)
                        ├── Reference data (static configuration)
                        └── Stats & activity (computed from DB)
                      → FhirDataAggregator (real clinical data)
                        └── Uses correct FHIR IDs (a-195900.E-{id})
```

### What already exists to build on:
- PostgreSQL + EF Core with `GatewayDbContext`, migrations, `WorkItemEntity`, `RegisteredPatientEntity`
- `PostgresWorkItemStore` — production-ready repository pattern to follow
- `FhirDataAggregator` — real clinical data from Athena sandbox
- `IntelligenceClient` — real LLM analysis (just wired in e2e-wiring PR)
- Redis `AnalysisResultStore` — caching layer for analysis results

## Design

### Layer 1: PA Request Entity (PostgreSQL)

New entity to persist PA requests in the database.

**New file:** `apps/gateway/Gateway.API/Data/Entities/PriorAuthRequestEntity.cs`

```csharp
public sealed class PriorAuthRequestEntity
{
    public required string Id { get; set; }                    // "PA-001", varchar(20)
    public required string PatientId { get; set; }             // Athena numeric ID: "60178"
    public required string FhirPatientId { get; set; }         // FHIR logical ID: "a-195900.E-60178"
    public required string PatientName { get; set; }
    public required string PatientMrn { get; set; }
    public string? PatientDob { get; set; }
    public string? PatientMemberId { get; set; }
    public string? PatientPayer { get; set; }
    public string? PatientAddress { get; set; }
    public string? PatientPhone { get; set; }
    public required string ProcedureCode { get; set; }         // CPT code
    public required string ProcedureName { get; set; }
    public string? DiagnosisCode { get; set; }                 // ICD-10
    public string? DiagnosisName { get; set; }
    public string? ProviderId { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderNpi { get; set; }
    public string? ServiceDate { get; set; }
    public string? PlaceOfService { get; set; }
    public string? ClinicalSummary { get; set; }
    public required string Status { get; set; }                // draft, ready, waiting_for_insurance, approved, denied
    public int Confidence { get; set; }
    public string? CriteriaJson { get; set; }                  // JSON array of {met, label, reason}
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ReadyAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public int ReviewTimeSeconds { get; set; }
}
```

**Criteria storage:** Store as JSON column (`jsonb` in PostgreSQL) rather than a separate table. This keeps the schema simple and matches the GraphQL response shape directly. Deserialize to `List<CriterionModel>` in the mapping layer.

**New file:** `apps/gateway/Gateway.API/Data/Configurations/PriorAuthRequestConfiguration.cs`

EF Core configuration with table name `prior_auth_requests`, indexes on `Status` and `CreatedAt`.

**New migration:** `AddPriorAuthRequests`

### Layer 2: PA Request Store (Repository)

**New file:** `apps/gateway/Gateway.API/Contracts/IPARequestStore.cs`

```csharp
public interface IPARequestStore
{
    Task<IReadOnlyList<PARequestModel>> GetAllAsync(CancellationToken ct = default);
    Task<PARequestModel?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PARequestModel> CreateAsync(PARequestModel request, CancellationToken ct = default);
    Task<PARequestModel?> UpdateAsync(string id, Action<PARequestModel> mutate, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<PAStatsModel> GetStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ActivityItemModel>> GetActivityAsync(CancellationToken ct = default);
}
```

**New file:** `apps/gateway/Gateway.API/Services/PostgresPARequestStore.cs`

Implementation using `GatewayDbContext` + `PriorAuthRequestEntity`. Follows the same patterns as `PostgresWorkItemStore`.

**Key design decisions:**
- Return `PARequestModel` directly (the GraphQL model) — no separate domain model needed since the GraphQL model IS the domain model for PA requests
- Map `CriteriaJson` ↔ `List<CriterionModel>` using System.Text.Json
- Compute stats via `GROUP BY Status` query
- Activity: `ORDER BY UpdatedAt DESC LIMIT 5`

### Layer 3: Reference Data (Static Configuration)

Reference data (procedures, medications, diagnoses, payers, providers) is **not patient-specific** — it's a static catalog. Keep this as configuration, not database.

**New file:** `apps/gateway/Gateway.API/Services/ReferenceDataService.cs`

```csharp
public sealed class ReferenceDataService
{
    public IReadOnlyList<ProcedureModel> Procedures { get; }
    public IReadOnlyList<MedicationModel> Medications { get; }
    public IReadOnlyList<DiagnosisModel> Diagnoses { get; }
    public IReadOnlyList<PayerModel> Payers { get; }
    public IReadOnlyList<ProviderModel> Providers { get; }
}
```

Extract the existing reference data arrays from `MockDataService` (lines 92-175) into this service. Registered as a singleton. No database needed — these are standard code sets (CPT, ICD-10, NPI).

### Layer 4: FHIR Patient ID Mapping

The Athena FHIR R4 API requires FHIR logical IDs in the format `a-{practiceId}.E-{patientId}`.

**Approach:** The dashboard already sends both `id` (numeric) and `fhirId` (FHIR format) in the `PatientInput`. Store both in the PA request entity:
- `PatientId` = `"60178"` (display, lookup)
- `FhirPatientId` = `"a-195900.E-60178"` (FHIR API calls)

**Changes needed:**
1. `CreatePARequestInput.PatientInput` — add `FhirId` field if not present
2. `CreatePARequest` mutation — store both IDs
3. `ProcessPARequest` mutation — use `FhirPatientId` when calling `FhirDataAggregator`
4. `FhirDataAggregator` receives the correct FHIR ID — no changes needed there

### Layer 5: GraphQL Resolver Updates

**File:** `apps/gateway/Gateway.API/GraphQL/Queries/Query.cs`

Replace `[Service] MockDataService` with `[Service] IPARequestStore` and `[Service] ReferenceDataService`:

```csharp
// Reference data (static)
public IReadOnlyList<ProcedureModel> GetProcedures([Service] ReferenceDataService refData)
    => refData.Procedures;

// PA request data (PostgreSQL)
public async Task<IReadOnlyList<PARequestModel>> GetPARequests(
    [Service] IPARequestStore store, CancellationToken ct)
    => await store.GetAllAsync(ct);
```

**File:** `apps/gateway/Gateway.API/GraphQL/Mutations/Mutation.cs`

Replace `[Service] MockDataService` with `[Service] IPARequestStore`:

```csharp
public async Task<PARequestModel> CreatePARequest(
    CreatePARequestInput input,
    [Service] IPARequestStore store,
    CancellationToken ct)
{
    var request = MapInputToModel(input);
    return await store.CreateAsync(request, ct);
}

public async Task<PARequestModel?> ProcessPARequest(
    string id,
    [Service] IPARequestStore store,
    [Service] IFhirDataAggregator fhirAggregator,
    [Service] IIntelligenceClient intelligenceClient,
    CancellationToken ct)
{
    var paRequest = await store.GetByIdAsync(id, ct);
    if (paRequest is null) return null;

    // Use FHIR patient ID for real FHIR data
    var clinicalBundle = await fhirAggregator.AggregateClinicalDataAsync(
        paRequest.FhirPatientId, cancellationToken: ct);
    // ... rest of pipeline unchanged
}
```

### Layer 6: Dashboard Patient ID Fix

**File:** `apps/dashboard/src/lib/patients.ts`

Already has both `id` and `fhirId`. Need to ensure the `CreatePARequestInput` sends `fhirId` to the backend.

**File:** `apps/dashboard/src/api/graphqlService.ts`

Update `PatientInput` in the `CREATE_PA_REQUEST` mutation to include `fhirId`.

**File:** `apps/gateway/Gateway.API/GraphQL/Inputs/CreatePARequestInput.cs`

Add `FhirId` to `PatientInput` record.

## What Gets Deleted

- `MockDataService.cs` (427 lines) — entirely replaced
- `MockDataServiceTests.cs` (489 lines) — replaced with `PARequestStoreTests`
- `LiveDataService.cs` (86 lines) — skeleton that was never used
- `IDataService.cs` — replaced by `IPARequestStore` + `ReferenceDataService`

## Migration Strategy

### Seed Data

The database starts empty. No seed data needed — users create PA requests through the dashboard. The home page will show an empty state until the user creates their first PA request.

**Alternative:** If an empty home page is undesirable, add an optional `--seed` flag to create a few sample PA requests on first startup (using real FHIR patient data from Athena).

## Testing Strategy

### Unit Tests
- `PostgresPARequestStoreTests` — CRUD operations with in-memory SQLite or test PostgreSQL
- `ReferenceDataServiceTests` — reference data completeness
- `ProcessPARequestMutationTests` — updated to use `IPARequestStore` mock instead of `MockDataService`
- `CreatePARequestMutationTests` — new, covering FHIR ID storage

### Integration Tests (Alba)
- Update existing `ProcessPARequestAlbaBootstrap` to register `IPARequestStore`
- Update `GatewayAlbaBootstrap` similarly
- Test PA lifecycle: create → process → submit → approve

### Dashboard Tests
- Existing component tests should pass unchanged (GraphQL contract preserved)
- Update `graphqlService.test.ts` if `PatientInput` shape changes

## File Change Summary

### New Files
| File | Purpose |
|------|---------|
| `Data/Entities/PriorAuthRequestEntity.cs` | PostgreSQL entity |
| `Data/Configurations/PriorAuthRequestConfiguration.cs` | EF Core config |
| `Data/Migrations/AddPriorAuthRequests.cs` | Database migration |
| `Data/Mappings/PriorAuthRequestMappings.cs` | Entity ↔ Model mapping |
| `Contracts/IPARequestStore.cs` | Repository interface |
| `Services/PostgresPARequestStore.cs` | PostgreSQL implementation |
| `Services/ReferenceDataService.cs` | Static reference data |

### Modified Files
| File | Change |
|------|--------|
| `Data/GatewayDbContext.cs` | Add `DbSet<PriorAuthRequestEntity>` |
| `GraphQL/Queries/Query.cs` | Replace MockDataService with IPARequestStore + ReferenceDataService |
| `GraphQL/Mutations/Mutation.cs` | Replace MockDataService with IPARequestStore, use FhirPatientId |
| `GraphQL/Inputs/CreatePARequestInput.cs` | Add FhirId to PatientInput |
| `GraphQL/Models/PARequestModel.cs` | Add FhirPatientId property |
| `DependencyExtensions.cs` | Register IPARequestStore, ReferenceDataService; remove MockDataService |
| `dashboard/src/api/graphqlService.ts` | Send fhirId in CreatePARequest mutation |

### Deleted Files
| File | Reason |
|------|--------|
| `Services/MockDataService.cs` | Replaced by PostgresPARequestStore + ReferenceDataService |
| `Services/IDataService.cs` | Replaced by IPARequestStore |
| `Services/LiveDataService.cs` | Never implemented skeleton |
| `Tests/Services/MockDataServiceTests.cs` | Replaced by PARequestStoreTests |

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Empty dashboard on first launch | Optional seed command, or accept empty state as correct |
| EF Core migration conflicts | Run migration fresh; existing tables unaffected |
| JSON criteria column queries | Use `jsonb` in PostgreSQL for indexing if needed |
| FHIR ID format varies by practice | Store both numeric and FHIR IDs; FHIR format is `a-{practiceId}.E-{patientId}` |
| Thread safety (MockDataService used locks) | PostgreSQL handles concurrency natively |
