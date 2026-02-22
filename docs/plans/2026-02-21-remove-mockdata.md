# Implementation Plan: Replace MockDataService with PostgreSQL Persistence

**Design:** `docs/designs/2026-02-21-remove-mockdata.md`
**Date:** 2026-02-21

## Parallelization Strategy

```
Wave 1 (parallel):
  ├── Worktree A: Tasks 1-3 (Entity + Config + Migration + Mapping)
  ├── Worktree B: Task 4 (ReferenceDataService)
  └── Worktree C: Task 5 (IPARequestStore interface)

Wave 2 (sequential, depends on Wave 1):
  └── Worktree D: Tasks 6-7 (PostgresPARequestStore implementation + tests)

Wave 3 (parallel, depends on Wave 2):
  ├── Worktree E: Tasks 8-9 (GraphQL Query + Mutation resolvers)
  └── Worktree F: Task 10 (FHIR patient ID fix in dashboard + PatientInput)

Wave 4 (sequential, depends on Wave 3):
  └── Worktree G: Tasks 11-12 (DI wiring + delete MockDataService + integration tests)
```

---

### Task 1: PriorAuthRequestEntity + EF Core Configuration

**Phase:** RED → GREEN

1. [RED] Write test: `PriorAuthRequestEntity_HasRequiredProperties`
   - File: `apps/gateway/Gateway.API.Tests/Data/PriorAuthRequestEntityTests.cs`
   - Tests: Entity can be constructed with all required properties, `CriteriaJson` nullable
   - Expected failure: `PriorAuthRequestEntity` does not exist

2. [GREEN] Create entity + configuration
   - File: `apps/gateway/Gateway.API/Data/Entities/PriorAuthRequestEntity.cs`
     - Properties: Id, PatientId, FhirPatientId, PatientName, PatientMrn, PatientDob, PatientMemberId, PatientPayer, PatientAddress, PatientPhone, ProcedureCode, ProcedureName, DiagnosisCode, DiagnosisName, ProviderId, ProviderName, ProviderNpi, ServiceDate, PlaceOfService, ClinicalSummary, Status, Confidence, CriteriaJson, CreatedAt, UpdatedAt, ReadyAt, SubmittedAt, ReviewTimeSeconds
   - File: `apps/gateway/Gateway.API/Data/Configurations/PriorAuthRequestConfiguration.cs`
     - Table: `prior_auth_requests`
     - PK: Id (varchar 20)
     - Indexes: Status, CreatedAt (descending), PatientId
     - CriteriaJson: `jsonb` column type

**Dependencies:** None
**Parallelizable:** Yes (Worktree A)

---

### Task 2: Database Migration

**Phase:** GREEN

1. [GREEN] Add DbSet to GatewayDbContext
   - File: `apps/gateway/Gateway.API/Data/GatewayDbContext.cs`
   - Add: `public DbSet<PriorAuthRequestEntity> PriorAuthRequests => Set<PriorAuthRequestEntity>();`
   - Add: `modelBuilder.ApplyConfiguration(new PriorAuthRequestConfiguration());`

2. [GREEN] Generate EF Core migration
   - Run: `dotnet ef migrations add AddPriorAuthRequests --project apps/gateway/Gateway.API`
   - File: `apps/gateway/Gateway.API/Data/Migrations/{timestamp}_AddPriorAuthRequests.cs`

**Dependencies:** Task 1
**Parallelizable:** No (same worktree as Task 1)

---

### Task 3: PriorAuthRequest Mapping Extensions

**Phase:** RED → GREEN

1. [RED] Write test: `PriorAuthRequestMappings_EntityToModel_MapsAllFields`
   - File: `apps/gateway/Gateway.API.Tests/Data/PriorAuthRequestMappingTests.cs`
   - Tests:
     - `ToModel_MapsAllScalarFields` — entity→PARequestModel with correct values
     - `ToModel_DeserializesCriteriaJson` — JSON array → `List<CriterionModel>`
     - `ToModel_HandlesNullCriteriaJson` — null → empty list
     - `ToEntity_MapsAllScalarFields` — PARequestModel→entity
     - `ToEntity_SerializesCriteriaToJson` — `List<CriterionModel>` → JSON
   - Expected failure: `PriorAuthRequestMappings` does not exist

2. [GREEN] Implement mappings
   - File: `apps/gateway/Gateway.API/Data/Mappings/PriorAuthRequestMappings.cs`
   - `ToModel(this PriorAuthRequestEntity)` → `PARequestModel`
     - Reconstruct `PatientModel` from flat entity fields
     - Deserialize `CriteriaJson` to `List<CriterionModel>`
     - Format DateTimeOffset to ISO string for CreatedAt/UpdatedAt/ReadyAt/SubmittedAt
   - `ToEntity(this PARequestModel, string? fhirPatientId)` → `PriorAuthRequestEntity`
     - Flatten `PatientModel` to entity fields
     - Serialize `Criteria` to JSON

**Dependencies:** Task 1
**Parallelizable:** No (same worktree as Tasks 1-2)

---

### Task 4: ReferenceDataService

**Phase:** RED → GREEN

1. [RED] Write tests:
   - File: `apps/gateway/Gateway.API.Tests/Services/ReferenceDataServiceTests.cs`
   - Tests:
     - `Procedures_ContainsExpectedItems` — at least 9 items with valid codes
     - `Medications_ContainsExpectedItems` — at least 6 items
     - `Diagnoses_ContainsExpectedItems` — at least 10 items with ICD-10 codes
     - `Payers_ContainsExpectedItems` — at least 5 items
     - `Providers_ContainsExpectedItems` — at least 3 items with NPI
     - `FindProcedureByCode_ReturnsMatch` — lookup by CPT code
     - `FindProviderById_ReturnsMatch` — lookup by provider ID
   - Expected failure: `ReferenceDataService` does not exist

2. [GREEN] Extract reference data from MockDataService
   - File: `apps/gateway/Gateway.API/Services/ReferenceDataService.cs`
   - Copy Procedures, Medications, Diagnoses, Payers, Providers arrays from MockDataService
   - Add lookup helpers: `FindProcedureByCode(string code)`, `FindProviderById(string id)`
   - Register as singleton

**Dependencies:** None
**Parallelizable:** Yes (Worktree B)

---

### Task 5: IPARequestStore Interface

**Phase:** RED → GREEN

1. [RED] Write test: `IPARequestStore_InterfaceDefinesRequiredMethods`
   - File: `apps/gateway/Gateway.API.Tests/Contracts/IPARequestStoreTests.cs`
   - Verify interface compiles with expected method signatures (compile-time check)
   - Expected failure: `IPARequestStore` does not exist

2. [GREEN] Define interface
   - File: `apps/gateway/Gateway.API/Contracts/IPARequestStore.cs`
   ```csharp
   public interface IPARequestStore
   {
       Task<IReadOnlyList<PARequestModel>> GetAllAsync(CancellationToken ct = default);
       Task<PARequestModel?> GetByIdAsync(string id, CancellationToken ct = default);
       Task<PARequestModel> CreateAsync(PARequestModel request, string fhirPatientId, CancellationToken ct = default);
       Task<PARequestModel?> UpdateFieldsAsync(string id, string? diagnosis, string? diagnosisCode, string? serviceDate, string? placeOfService, string? clinicalSummary, IReadOnlyList<CriterionModel>? criteria, CancellationToken ct = default);
       Task<PARequestModel?> ApplyAnalysisResultAsync(string id, string clinicalSummary, int confidence, IReadOnlyList<CriterionModel> criteria, CancellationToken ct = default);
       Task<PARequestModel?> SubmitAsync(string id, int addReviewTimeSeconds = 0, CancellationToken ct = default);
       Task<PARequestModel?> AddReviewTimeAsync(string id, int seconds, CancellationToken ct = default);
       Task<bool> DeleteAsync(string id, CancellationToken ct = default);
       Task<PAStatsModel> GetStatsAsync(CancellationToken ct = default);
       Task<IReadOnlyList<ActivityItemModel>> GetActivityAsync(CancellationToken ct = default);
   }
   ```

**Dependencies:** None
**Parallelizable:** Yes (Worktree C)

---

### Task 6: PostgresPARequestStore Implementation

**Phase:** RED → GREEN

1. [RED] Write tests:
   - File: `apps/gateway/Gateway.API.Tests/Services/PostgresPARequestStoreTests.cs`
   - Use in-memory SQLite or Npgsql test container
   - Tests:
     - `CreateAsync_PersistsRequest_ReturnsWithId`
     - `GetByIdAsync_ExistingId_ReturnsRequest`
     - `GetByIdAsync_NonExistentId_ReturnsNull`
     - `GetAllAsync_ReturnsAllRequests_OrderedByCreatedAtDesc`
     - `UpdateFieldsAsync_UpdatesDiagnosisAndCriteria`
     - `UpdateFieldsAsync_NonExistentId_ReturnsNull`
     - `ApplyAnalysisResultAsync_SetsReadyStatusAndConfidence`
     - `ApplyAnalysisResultAsync_SetsReadyAtTimestamp`
     - `SubmitAsync_SetsWaitingForInsuranceStatus`
     - `SubmitAsync_SetsSubmittedAtTimestamp`
     - `AddReviewTimeAsync_IncrementsSeconds`
     - `DeleteAsync_ExistingId_ReturnsTrue`
     - `DeleteAsync_NonExistentId_ReturnsFalse`
     - `GetStatsAsync_CountsByStatus`
     - `GetActivityAsync_ReturnsLast5OrderedByUpdatedAt`
   - Expected failure: `PostgresPARequestStore` does not exist

2. [GREEN] Implement store
   - File: `apps/gateway/Gateway.API/Services/PostgresPARequestStore.cs`
   - Follow `PostgresWorkItemStore` patterns:
     - Constructor takes `GatewayDbContext`
     - Use `AsNoTracking()` for reads
     - Use `FirstOrDefaultAsync` for updates
     - Auto-generate IDs: `$"PA-{counter:D3}"` (via max ID query or sequence)
   - Stats: SQL `GROUP BY Status` aggregation
   - Activity: `ORDER BY UpdatedAt DESC LIMIT 5`, map to `ActivityItemModel`

**Dependencies:** Tasks 1-3, 5
**Parallelizable:** Yes (Worktree D, after Wave 1)

---

### Task 7: PostgresPARequestStore Edge Cases + Criteria JSON

**Phase:** RED → GREEN

1. [RED] Write tests:
   - File: `apps/gateway/Gateway.API.Tests/Services/PostgresPARequestStoreTests.cs` (extend)
   - Tests:
     - `CreateAsync_WithCriteria_SerializesToJsonb`
     - `GetByIdAsync_WithCriteria_DeserializesFromJsonb`
     - `ApplyAnalysisResultAsync_WithMixedCriteriaStatuses_PreservesAll`
     - `GetStatsAsync_EmptyDatabase_ReturnsZeros`
     - `GetActivityAsync_EmptyDatabase_ReturnsEmptyList`
     - `CreateAsync_GeneratesSequentialIds`

2. [GREEN] Handle edge cases in implementation

**Dependencies:** Task 6
**Parallelizable:** No (same worktree as Task 6)

---

### Task 8: GraphQL Query Resolvers

**Phase:** RED → GREEN

1. [RED] Write tests:
   - File: `apps/gateway/Gateway.API.Tests/GraphQL/QueryResolverTests.cs`
   - Tests:
     - `GetProcedures_ReturnsFromReferenceDataService`
     - `GetMedications_ReturnsFromReferenceDataService`
     - `GetDiagnoses_ReturnsFromReferenceDataService`
     - `GetPARequests_CallsStoreGetAllAsync`
     - `GetPARequest_CallsStoreGetByIdAsync`
     - `GetPAStats_CallsStoreGetStatsAsync`
     - `GetActivity_CallsStoreGetActivityAsync`
   - Expected failure: Query resolvers still inject MockDataService

2. [GREEN] Update Query.cs
   - File: `apps/gateway/Gateway.API/GraphQL/Queries/Query.cs`
   - Replace `[Service] MockDataService` with:
     - `[Service] ReferenceDataService refData` for procedures/medications/diagnoses/payers/providers
     - `[Service] IPARequestStore store` for PA requests/stats/activity
   - Make PA queries async (return `Task<>`)

**Dependencies:** Tasks 4, 5, 6
**Parallelizable:** Yes (Worktree E)

---

### Task 9: GraphQL Mutation Resolvers

**Phase:** RED → GREEN

1. [RED] Write tests:
   - File: `apps/gateway/Gateway.API.Tests/GraphQL/MutationResolverTests.cs` (update existing ProcessPARequestMutationTests)
   - Tests:
     - `CreatePARequest_CallsStoreWithFhirPatientId`
     - `CreatePARequest_MapsInputFieldsCorrectly`
     - `UpdatePARequest_CallsStoreUpdateFieldsAsync`
     - `ProcessPARequest_UsesFhirPatientIdForFhirAggregator`
     - `ProcessPARequest_CallsStoreApplyAnalysisResultAsync`
     - `SubmitPARequest_CallsStoreSubmitAsync`
     - `DeletePARequest_CallsStoreDeleteAsync`
   - Expected failure: Mutations still inject MockDataService

2. [GREEN] Update Mutation.cs
   - File: `apps/gateway/Gateway.API/GraphQL/Mutations/Mutation.cs`
   - Replace `[Service] MockDataService` with `[Service] IPARequestStore store` and `[Service] ReferenceDataService refData`
   - `CreatePARequest`:
     - Look up procedure/provider from ReferenceDataService
     - Build PARequestModel with `FhirPatientId` from `input.Patient.FhirId`
     - Call `store.CreateAsync(model, fhirPatientId)`
   - `ProcessPARequest`:
     - Get request from store: `store.GetByIdAsync(id)`
     - Use `paRequest.FhirPatientId` (new field on PARequestModel) for FHIR aggregation
     - Call `store.ApplyAnalysisResultAsync()` instead of `mockData.ApplyAnalysisResult()`
   - `SubmitPARequest`: Call `store.SubmitAsync(id, addReviewTimeSeconds)`

**Dependencies:** Tasks 4, 5, 6
**Parallelizable:** Yes (Worktree E, same as Task 8)

---

### Task 10: FHIR Patient ID Fix (Dashboard + Model)

**Phase:** RED → GREEN

1. [RED] Write tests:
   - File: `apps/dashboard/src/api/__tests__/graphqlService.test.ts` (extend)
   - Tests:
     - `useCreatePARequest sends fhirId in patient input`
   - File: `apps/gateway/Gateway.API.Tests/GraphQL/MutationResolverTests.cs` (extend)
   - Tests:
     - `CreatePARequest_StoresFhirPatientId_FromInput`
     - `ProcessPARequest_PassesFhirPatientId_ToFhirAggregator`

2. [GREEN] Add FhirPatientId to PARequestModel
   - File: `apps/gateway/Gateway.API/GraphQL/Models/PARequestModel.cs`
   - Add: `public string? FhirPatientId { get; init; }`
   - File: `apps/dashboard/src/api/graphqlService.ts`
   - Ensure `CREATE_PA_REQUEST` mutation sends `fhirId` from patient input
   - Add `fhirPatientId` to `PARequestFields` fragment

**Dependencies:** None (model changes are additive)
**Parallelizable:** Yes (Worktree F)

---

### Task 11: DI Wiring + Delete MockDataService

**Phase:** RED → GREEN → REFACTOR

1. [RED] Write test:
   - File: `apps/gateway/Gateway.API.Tests/DependencyExtensionsTests.cs` (extend)
   - Tests:
     - `AddGatewayPersistence_RegistersIPARequestStore`
     - `AddGatewayServices_RegistersReferenceDataService`
     - `AddGatewayServices_DoesNotRegisterMockDataService`

2. [GREEN] Update DI registration
   - File: `apps/gateway/Gateway.API/DependencyExtensions.cs`
   - Replace `services.AddSingleton<MockDataService>()` with:
     - `services.AddSingleton<ReferenceDataService>()`
     - `services.AddScoped<IPARequestStore, PostgresPARequestStore>()` (in AddGatewayPersistence)

3. [REFACTOR] Delete dead code
   - Delete: `apps/gateway/Gateway.API/Services/MockDataService.cs`
   - Delete: `apps/gateway/Gateway.API/Services/IDataService.cs`
   - Delete: `apps/gateway/Gateway.API/Services/LiveDataService.cs`
   - Delete: `apps/gateway/Gateway.API.Tests/Services/MockDataServiceTests.cs`

**Dependencies:** Tasks 6-10 (all consumers updated first)
**Parallelizable:** No (final assembly)

---

### Task 12: Alba Integration Tests

**Phase:** RED → GREEN

1. [RED] Write tests:
   - File: `apps/gateway/Gateway.API.Tests/Integration/PARequestLifecycleIntegrationTests.cs`
   - Uses Alba with in-memory SQLite (or Npgsql test container)
   - Tests:
     - `CreatePARequest_PersistsToDatabase_ReturnsInGetPARequests`
     - `ProcessPARequest_WithFhirPatientId_CallsIntelligenceAndPersists`
     - `SubmitPARequest_UpdatesStatusToWaitingForInsurance`
     - `GetPAStats_ReflectsCurrentDatabaseState`
     - `DeletePARequest_RemovesFromDatabase`

2. [GREEN] Create Alba bootstrap for PostgreSQL-backed store
   - File: `apps/gateway/Gateway.API.Tests/Integration/PARequestLifecycleAlbaBootstrap.cs`
   - Register in-memory SQLite DbContext + mock FHIR + mock Intelligence
   - Verify full lifecycle through GraphQL

**Dependencies:** Tasks 8-11
**Parallelizable:** No (final validation)

---

## Summary

| Task | Description | Worktree | Wave | Dependencies |
|------|------------|----------|------|--------------|
| 1 | PriorAuthRequestEntity + Configuration | A | 1 | None |
| 2 | Database Migration | A | 1 | Task 1 |
| 3 | Mapping Extensions | A | 1 | Task 1 |
| 4 | ReferenceDataService | B | 1 | None |
| 5 | IPARequestStore Interface | C | 1 | None |
| 6 | PostgresPARequestStore Implementation | D | 2 | Tasks 1-3, 5 |
| 7 | Store Edge Cases + Criteria JSON | D | 2 | Task 6 |
| 8 | GraphQL Query Resolvers | E | 3 | Tasks 4, 5, 6 |
| 9 | GraphQL Mutation Resolvers | E | 3 | Tasks 4, 5, 6 |
| 10 | FHIR Patient ID Fix | F | 3 | None |
| 11 | DI Wiring + Delete MockDataService | G | 4 | Tasks 6-10 |
| 12 | Alba Integration Tests | G | 4 | Tasks 8-11 |

**Total:** 12 tasks, 7 worktrees, 4 waves
**Estimated test count:** ~45 new tests across 6 test files
