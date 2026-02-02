# Implementation Plan: Observability, Persistence & Test Coverage

## Source Design
Link: `docs/designs/2026-02-02-observability-persistence-coverage.md`

## Scope
**Target:** Full design
**Excluded:** None

## Summary
- Total tasks: 15
- Parallel groups: 3
- Estimated test count: 24
- Design coverage: 14/14 sections covered (including Alba integration tests)

## Spec Traceability

### Traceability Matrix

| Design Section | Key Requirements | Task ID(s) | Status |
|----------------|-----------------|------------|--------|
| Observability > Signal Logging Pattern | - Add structured logs showing bundle counts without PHI | 001 | Covered |
| Observability > Validation Signals | - Add boolean flags for required data presence | 002 | Covered |
| Persistence > EF Core Packages | - Add Aspire.Npgsql.EntityFrameworkCore.PostgreSQL | 003 | Covered |
| Persistence > WorkItemEntity | - Create entity, configuration, mapping | 004 | Covered |
| Persistence > RegisteredPatientEntity | - Create entity, configuration, mapping | 005 | Covered |
| Persistence > GatewayDbContext | - Create DbContext with DbSets | 006 | Covered |
| Persistence > Initial Migration | - Generate EF Core migration | 007 | Covered |
| Persistence > PostgresWorkItemStore | - Implement IWorkItemStore with EF Core | 008, 009 | Covered |
| Persistence > PostgresPatientRegistry | - Implement IPatientRegistry with EF Core | 010, 011 | Covered |
| Persistence > DI Registration | - Update DependencyExtensions, Program.cs | 012 | Covered |
| Coverage > Coverlet Package | - Add coverlet.collector to test project | 013 | Covered |
| Coverage > CI Configuration | - Update CI with coverage threshold | 013 | Covered |
| Testing Strategy | - Unit tests for Postgres stores<br>- Alba integration tests | 008, 010, 014, 015 | Covered |

### Open Questions Resolution

| Question | Resolution |
|----------|------------|
| Support both in-memory and PostgreSQL via config? | Deferred: Production uses PostgreSQL; in-memory remains for tests. No config switch needed. |
| Soft-delete for audit trail? | Deferred: Create GitHub issue for future work. Hard-delete sufficient for MVP. |

## Task Breakdown

### Task 001: Add signal logging to FhirDataAggregator

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `AggregateClinicalDataAsync_WithData_LogsSignalCounts`
   - File: `apps/gateway/Gateway.API.Tests/Services/FhirDataAggregatorTests.cs`
   - Expected failure: No test file exists or test fails because signal log not present
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement signal logging after aggregation
   - File: `apps/gateway/Gateway.API/Services/FhirDataAggregator.cs`
   - Add structured log with HasPatientDemographics flag
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up (optional)
   - Apply: Consistent log message format
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group A)

---

### Task 002: Add data validation logging to EncounterProcessor

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `ProcessAsync_WithClinicalData_LogsValidationSignals`
   - File: `apps/gateway/Gateway.API.Tests/Services/EncounterProcessorTests.cs`
   - Expected failure: No test verifying validation logging exists
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement validation signal logging
   - File: `apps/gateway/Gateway.API/Services/EncounterProcessor.cs`
   - Add HasRequiredData validation before Intelligence call
   - Log PatientPresent, ConditionsPresent, ProcedureCodePresent flags
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up (optional)
   - Apply: Extract validation logic to helper if complex
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes (Group A)

---

### Task 003: Add EF Core packages to Gateway.API

**Phase:** GREEN (No test needed - package reference)

**TDD Steps:**
1. [GREEN] Add package reference
   - File: `apps/gateway/Gateway.API/Gateway.API.csproj`
   - Add: `<PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="13.1.0" />`
   - Run: `dotnet restore` - MUST SUCCEED

**Verification:**
- [ ] Package restores successfully
- [ ] No build errors

**Dependencies:** None
**Parallelizable:** Yes (Group B)

---

### Task 004: Create WorkItemEntity and configuration

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `ToModel_ValidEntity_MapsAllProperties`
   - File: `apps/gateway/Gateway.API.Tests/Data/Entities/WorkItemEntityTests.cs`
   - Expected failure: WorkItemEntity class does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Create entity, configuration, and mapping
   - Files:
     - `apps/gateway/Gateway.API/Data/Entities/WorkItemEntity.cs`
     - `apps/gateway/Gateway.API/Data/Configurations/WorkItemConfiguration.cs`
     - `apps/gateway/Gateway.API/Data/Mappings/WorkItemMappings.cs`
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up (optional)
   - Apply: Ensure readonly properties where appropriate
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** 003
**Parallelizable:** No (sequential with 003)

---

### Task 005: Create RegisteredPatientEntity and configuration

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `ToModel_ValidEntity_MapsAllProperties`
   - File: `apps/gateway/Gateway.API.Tests/Data/Entities/RegisteredPatientEntityTests.cs`
   - Expected failure: RegisteredPatientEntity class does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Create entity, configuration, and mapping
   - Files:
     - `apps/gateway/Gateway.API/Data/Entities/RegisteredPatientEntity.cs`
     - `apps/gateway/Gateway.API/Data/Configurations/RegisteredPatientConfiguration.cs`
     - `apps/gateway/Gateway.API/Data/Mappings/RegisteredPatientMappings.cs`
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up (optional)
   - Apply: Consistent naming with WorkItemEntity
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** 003
**Parallelizable:** Yes (can run parallel with 004 after 003)

---

### Task 006: Create GatewayDbContext

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `GatewayDbContext_HasWorkItemsDbSet`
   - File: `apps/gateway/Gateway.API.Tests/Data/GatewayDbContextTests.cs`
   - Expected failure: GatewayDbContext does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Create DbContext with DbSets
   - File: `apps/gateway/Gateway.API/Data/GatewayDbContext.cs`
   - Include DbSet<WorkItemEntity> and DbSet<RegisteredPatientEntity>
   - Apply configurations in OnModelCreating
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up (optional)
   - Apply: Standard DbContext patterns
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** 004, 005
**Parallelizable:** No (requires entities)

---

### Task 007: Create initial EF Core migration

**Phase:** GREEN (Migration generation)

**TDD Steps:**
1. [GREEN] Generate migration
   - Command: `dotnet ef migrations add InitialCreate --project apps/gateway/Gateway.API --output-dir Data/Migrations`
   - File: `apps/gateway/Gateway.API/Data/Migrations/*_InitialCreate.cs`
   - Run: `dotnet build` - MUST SUCCEED

**Verification:**
- [ ] Migration file generated
- [ ] Migration includes work_items and registered_patients tables
- [ ] Indexes created as specified in design

**Dependencies:** 006
**Parallelizable:** No (requires DbContext)

---

### Task 008: Implement PostgresWorkItemStore (tests)

**Phase:** RED

**TDD Steps:**
1. [RED] Write tests for all IWorkItemStore methods
   - File: `apps/gateway/Gateway.API.Tests/Services/PostgresWorkItemStoreTests.cs`
   - Tests:
     - `CreateAsync_ValidWorkItem_PersistsToDatabase`
     - `GetByIdAsync_ExistingId_ReturnsWorkItem`
     - `GetByIdAsync_NonExistentId_ReturnsNull`
     - `UpdateStatusAsync_ExistingId_UpdatesStatus`
     - `UpdateAsync_ExistingId_UpdatesAllFields`
     - `GetByEncounterAsync_MatchingEncounter_ReturnsWorkItems`
     - `GetAllAsync_WithFilters_ReturnsFilteredResults`
   - Expected failure: PostgresWorkItemStore does not exist
   - Run: `dotnet test` - MUST FAIL

**Verification:**
- [ ] All tests written
- [ ] Tests fail for the right reason (class doesn't exist)

**Dependencies:** 006
**Parallelizable:** Yes (Group C - after 006)

---

### Task 009: Implement PostgresWorkItemStore (implementation)

**Phase:** GREEN → REFACTOR

**TDD Steps:**
1. [GREEN] Implement PostgresWorkItemStore
   - File: `apps/gateway/Gateway.API/Services/PostgresWorkItemStore.cs`
   - Implement all IWorkItemStore methods using DbContext
   - Run: `dotnet test` - MUST PASS

2. [REFACTOR] Clean up (optional)
   - Apply: Use async LINQ methods, optimize queries
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] All tests pass
- [ ] No extra code beyond test requirements

**Dependencies:** 008
**Parallelizable:** No (requires tests)

---

### Task 010: Implement PostgresPatientRegistry (tests)

**Phase:** RED

**TDD Steps:**
1. [RED] Write tests for all IPatientRegistry methods
   - File: `apps/gateway/Gateway.API.Tests/Services/PostgresPatientRegistryTests.cs`
   - Tests:
     - `RegisterAsync_ValidPatient_PersistsToDatabase`
     - `GetAsync_ExistingPatient_ReturnsPatient`
     - `GetAsync_NonExistentPatient_ReturnsNull`
     - `GetActiveAsync_WithActivePatients_ReturnsActive`
     - `UnregisterAsync_ExistingPatient_RemovesFromDatabase`
     - `UpdateAsync_ExistingPatient_UpdatesFields`
   - Expected failure: PostgresPatientRegistry does not exist
   - Run: `dotnet test` - MUST FAIL

**Verification:**
- [ ] All tests written
- [ ] Tests fail for the right reason (class doesn't exist)

**Dependencies:** 006
**Parallelizable:** Yes (Group C - after 006)

---

### Task 011: Implement PostgresPatientRegistry (implementation)

**Phase:** GREEN → REFACTOR

**TDD Steps:**
1. [GREEN] Implement PostgresPatientRegistry
   - File: `apps/gateway/Gateway.API/Services/PostgresPatientRegistry.cs`
   - Implement all IPatientRegistry methods using DbContext
   - Run: `dotnet test` - MUST PASS

2. [REFACTOR] Clean up (optional)
   - Apply: Use async LINQ methods, optimize queries
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] All tests pass
- [ ] No extra code beyond test requirements

**Dependencies:** 010
**Parallelizable:** No (requires tests)

---

### Task 012: Update DI registration for PostgreSQL persistence

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `AddGatewayPersistence_RegistersPostgresStores`
   - File: `apps/gateway/Gateway.API.Tests/DependencyExtensionsTests.cs`
   - Expected failure: AddGatewayPersistence method doesn't exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement DI registration
   - Files:
     - `apps/gateway/Gateway.API/DependencyExtensions.cs` (add AddGatewayPersistence method)
     - `apps/gateway/Gateway.API/Program.cs` (call AddGatewayPersistence, add migration on dev)
   - Register DbContext, PostgresWorkItemStore, PostgresPatientRegistry
   - Replace InMemory registrations with Postgres implementations
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up (optional)
   - Apply: Consistent extension method naming
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** 009, 011
**Parallelizable:** No (requires stores)

---

### Task 013: Add test coverage to CI

**Phase:** GREEN (CI configuration)

**TDD Steps:**
1. [GREEN] Add coverlet.collector package
   - File: `apps/gateway/Gateway.API.Tests/Gateway.API.Tests.csproj`
   - Add: `<PackageReference Include="coverlet.collector" Version="6.0.4" />`

2. [GREEN] Update CI workflow
   - File: `.github/workflows/ci.yml`
   - Update gateway-build job:
     - Add `--collect:"XPlat Code Coverage"` to test command
     - Add coverage threshold check step (60%)
     - Update artifact upload path
   - Run: Local `dotnet test` with coverage - MUST SUCCEED

**Verification:**
- [ ] Coverage report generated locally
- [ ] CI workflow syntax valid

**Dependencies:** None
**Parallelizable:** Yes (Group A)

---

### Task 014: Update Alba bootstrap for EF Core persistence

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `GatewayAlbaBootstrap_UsesEfCoreInMemoryDatabase`
   - File: `apps/gateway/Gateway.API.Tests/Integration/GatewayAlbaBootstrapTests.cs`
   - Expected failure: Bootstrap doesn't configure EF Core
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Update Alba bootstrap to use EF Core with SQLite in-memory
   - File: `apps/gateway/Gateway.API.Tests/Integration/GatewayAlbaBootstrap.cs`
   - Configure SQLite in-memory provider for GatewayDbContext
   - Register PostgresWorkItemStore and PostgresPatientRegistry
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up (optional)
   - Apply: Ensure database is created fresh per test session
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] Existing Alba integration tests still pass

**Dependencies:** 012
**Parallelizable:** No (requires DI registration complete)

---

### Task 015: Add Alba integration tests for persistence behavior

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write integration tests verifying persistence
   - File: `apps/gateway/Gateway.API.Tests/Integration/PersistenceIntegrationTests.cs`
   - Tests:
     - `WorkItem_CreateAndRetrieve_DataPersistsAcrossRequests`
     - `WorkItem_UpdateStatus_ChangePersists`
     - `PatientRegistration_RegisterAndGet_DataPersists`
     - `PatientRegistration_Unregister_RemovesFromDatabase`
   - Expected failure: Tests fail due to persistence issues or missing setup
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Ensure Alba bootstrap creates database correctly
   - Verify SQLite in-memory database is properly initialized
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Clean up (optional)
   - Apply: Extract common test fixtures if needed
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] All persistence tests pass
- [ ] Tests verify data survives across multiple API calls
- [ ] Tests clean up properly between test runs

**Dependencies:** 014
**Parallelizable:** No (requires Alba bootstrap update)

---

## Parallelization Strategy

### Group A: Observability & Coverage (Independent)
Can run immediately in parallel:
- Task 001: Signal logging (FhirDataAggregator)
- Task 002: Validation logging (EncounterProcessor)
- Task 013: CI coverage configuration

### Group B: EF Core Foundation (Sequential)
Must run in order:
- Task 003: Add EF Core packages
- Task 004: WorkItemEntity (after 003)
- Task 005: RegisteredPatientEntity (after 003, parallel with 004)
- Task 006: GatewayDbContext (after 004, 005)
- Task 007: Initial migration (after 006)

### Group C: Postgres Stores (After Group B)
Can run in parallel after Group B completes:
- Tasks 008-009: PostgresWorkItemStore (tests then implementation)
- Tasks 010-011: PostgresPatientRegistry (tests then implementation)

### Final: DI Registration & Integration Tests
- Task 012: Update DI (after 009, 011)
- Task 014: Alba bootstrap update (after 012)
- Task 015: Alba persistence tests (after 014)

```
Group A (parallel):          Group B (sequential):
[001] ─┐                     [003] → [004] ─┬─→ [006] → [007]
[002] ─┼─→ (independent)            └─[005]─┘
[013] ─┘                                        ↓
                                          Group C (parallel):
                                          [008] → [009] ─┬─→ [012] → [014] → [015]
                                          [010] → [011] ─┘
```

## Deferred Items

| Item | Rationale |
|------|-----------|
| Redis caching for SSE | Out of scope - create GitHub issue for multi-instance support |
| Soft-delete for audit | Out of scope - create GitHub issue for compliance requirements |
| Testcontainers for PostgreSQL | Deferred - using SQLite in-memory for Alba integration tests; Testcontainers can be added later for true PostgreSQL parity |

## Completion Checklist
- [ ] All tests written before implementation
- [ ] All tests pass
- [ ] Code coverage meets standards
- [ ] Ready for review
