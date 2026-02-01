# Implementation Plan: Gateway ServiceRequest Hydration

## Source Design
Link: `docs/designs/2026-01-29-athenahealth-pivot-mvp.md`
Reconciliation: `docs/pitch/design-reconciliation.md`

## Scope
**Gateway API (.NET) only** — ServiceRequest hydration, work item states, re-hydration endpoint

## Summary
- Total tasks: 12
- Parallel groups: 3
- Estimated test count: ~18

## Task Breakdown

---

### Task 001: Create ServiceRequestInfo Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `ServiceRequestInfo_RequiredProperties_InitializesCorrectly`
   - File: `apps/gateway/Gateway.API.Tests/Models/ServiceRequestInfoTests.cs`
   - Expected failure: Type `ServiceRequestInfo` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/ServiceRequestInfo.cs`
   - Changes: Create record with `Id`, `Status`, `Code` (CodeableConcept), `EncounterId`, `AuthoredOn`
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Add XML documentation
   - Apply: Document all public members
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 002: Add ServiceRequests Property to ClinicalBundle

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `ClinicalBundle_ServiceRequests_DefaultsToEmptyList`
   - File: `apps/gateway/Gateway.API.Tests/Models/ClinicalBundleTests.cs`
   - Expected failure: Property `ServiceRequests` does not exist on `ClinicalBundle`
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/ClinicalBundle.cs`
   - Changes: Add `public List<ServiceRequestInfo> ServiceRequests { get; init; } = [];`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 001
**Parallelizable:** No (depends on 001)

---

### Task 003: Add SearchServiceRequestsAsync to IFhirClient Interface

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `IFhirClient_HasSearchServiceRequestsAsyncMethod`
   - File: `apps/gateway/Gateway.API.Tests/Contracts/IFhirClientTests.cs`
   - Expected failure: Method `SearchServiceRequestsAsync` does not exist on interface
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Contracts/IFhirClient.cs`
   - Changes: Add method signature with `patientId`, `encounterId?`, `accessToken`, `ct` parameters
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 001
**Parallelizable:** Yes (parallel with 002)

---

### Task 004: Implement SearchServiceRequestsAsync in FhirClient

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `SearchServiceRequestsAsync_ValidBundle_ExtractsServiceRequests`
   - File: `apps/gateway/Gateway.API.Tests/Services/FhirClientTests.cs`
   - Expected failure: Method not implemented (throws `NotImplementedException`)
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `SearchServiceRequestsAsync_ExtractsCptCode_FromCodeableConcept`
   - File: `apps/gateway/Gateway.API.Tests/Services/FhirClientTests.cs`
   - Expected failure: CPT code extraction not implemented
   - Run: `dotnet test` - MUST FAIL

3. [RED] Write test: `SearchServiceRequestsAsync_FiltersbyEncounter_WhenProvided`
   - File: `apps/gateway/Gateway.API.Tests/Services/FhirClientTests.cs`
   - Expected failure: Encounter filtering not implemented
   - Run: `dotnet test` - MUST FAIL

4. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Services/FhirClient.cs`
   - Changes: Implement JSON extraction for ServiceRequest resources, query construction with optional encounter filter
   - Run: `dotnet test` - MUST PASS

5. [REFACTOR] Extract JSON parsing helper if needed
   - Apply: DRY principle for CodeableConcept extraction
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 003
**Parallelizable:** No (depends on 003)

---

### Task 005: Update FhirDataAggregator to Fetch ServiceRequests

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `AggregateClinicalDataAsync_IncludesServiceRequests_InBundle`
   - File: `apps/gateway/Gateway.API.Tests/Services/FhirDataAggregatorTests.cs`
   - Expected failure: ServiceRequests not populated in returned bundle
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Services/FhirDataAggregator.cs`
   - Changes: Add `_fhirClient.SearchServiceRequestsAsync()` to parallel fetch, assign to bundle
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 004, Task 002
**Parallelizable:** No (depends on 004 and 002)

---

### Task 006: Create WorkItemStatus Enum

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `WorkItemStatus_HasExpectedValues`
   - File: `apps/gateway/Gateway.API.Tests/Models/WorkItemStatusTests.cs`
   - Expected failure: Type `WorkItemStatus` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/WorkItemStatus.cs`
   - Changes: Create enum with `ReadyForReview`, `MissingData`, `PayerRequirementsNotMet`, `Submitted`, `NoPaRequired`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 007: Create WorkItem Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `WorkItem_RequiredProperties_InitializesCorrectly`
   - File: `apps/gateway/Gateway.API.Tests/Models/WorkItemTests.cs`
   - Expected failure: Type `WorkItem` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/WorkItem.cs`
   - Changes: Create record with `Id`, `EncounterId`, `PatientId`, `ServiceRequestId`, `Status`, `ProcedureCode`, `CreatedAt`, `UpdatedAt`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 006
**Parallelizable:** No (depends on 006)

---

### Task 008: Create IWorkItemStore Interface

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `IWorkItemStore_HasRequiredMethods`
   - File: `apps/gateway/Gateway.API.Tests/Contracts/IWorkItemStoreTests.cs`
   - Expected failure: Interface `IWorkItemStore` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Contracts/IWorkItemStore.cs`
   - Changes: Define `CreateAsync`, `GetByIdAsync`, `UpdateStatusAsync`, `GetByEncounterAsync` methods
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 007
**Parallelizable:** No (depends on 007)

---

### Task 009: Implement InMemoryWorkItemStore

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `CreateAsync_StoresWorkItem_ReturnsId`
   - File: `apps/gateway/Gateway.API.Tests/Services/InMemoryWorkItemStoreTests.cs`
   - Expected failure: Class `InMemoryWorkItemStore` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `GetByIdAsync_ExistingItem_ReturnsWorkItem`
   - Expected failure: Get method returns null for all queries
   - Run: `dotnet test` - MUST FAIL

3. [RED] Write test: `UpdateStatusAsync_ValidTransition_UpdatesStatus`
   - Expected failure: Update not implemented
   - Run: `dotnet test` - MUST FAIL

4. [RED] Write test: `UpdateStatusAsync_InvalidTransition_ReturnsFalse`
   - Expected failure: No state transition validation
   - Run: `dotnet test` - MUST FAIL

5. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Services/InMemoryWorkItemStore.cs`
   - Changes: Implement dictionary-based storage with CRUD operations
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 008
**Parallelizable:** No (depends on 008)

---

### Task 010: Create RehydrateRequest Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `RehydrateRequest_RequiredProperties_InitializesCorrectly`
   - File: `apps/gateway/Gateway.API.Tests/Models/RehydrateRequestTests.cs`
   - Expected failure: Type `RehydrateRequest` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/RehydrateRequest.cs`
   - Changes: Create record with `WorkItemId`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 011: Implement POST /work-items/{id}/rehydrate Endpoint

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `RehydrateEndpoint_ValidWorkItem_ReturnsOk`
   - File: `apps/gateway/Gateway.API.Tests/Endpoints/WorkItemEndpointsTests.cs`
   - Expected failure: Endpoint not registered
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `RehydrateEndpoint_NotFoundWorkItem_Returns404`
   - Expected failure: No work item lookup
   - Run: `dotnet test` - MUST FAIL

3. [RED] Write test: `RehydrateEndpoint_TriggersReanalysis_UpdatesWorkItem`
   - Expected failure: Re-analysis not triggered
   - Run: `dotnet test` - MUST FAIL

4. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Endpoints/WorkItemEndpoints.cs`
   - Changes: Create endpoint that fetches work item, re-hydrates via aggregator, re-analyzes via intelligence client, updates status
   - Run: `dotnet test` - MUST PASS

5. [REFACTOR] Extract rehydration logic to service
   - Apply: SRP - create `IRehydrationService` if endpoint becomes too complex
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 005, Task 009
**Parallelizable:** No (depends on 005 and 009)

---

### Task 012: Register New Services in DI Container

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `ServiceCollection_ResolvesIWorkItemStore`
   - File: `apps/gateway/Gateway.API.Tests/DependencyExtensionsTests.cs`
   - Expected failure: Service not registered
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/DependencyExtensions.cs`
   - Changes: Register `InMemoryWorkItemStore` as `IWorkItemStore` (singleton)
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 009
**Parallelizable:** No (depends on 009)

---

## Parallelization Strategy

### Sequential Chain A: ServiceRequest Hydration
```
Task 001 (ServiceRequestInfo)
  → Task 002 (ClinicalBundle property)
    → Task 005 (FhirDataAggregator)
      → Task 011 (Rehydrate endpoint)
```

### Sequential Chain B: IFhirClient Extension
```
Task 001 (ServiceRequestInfo)
  → Task 003 (IFhirClient interface)
    → Task 004 (FhirClient implementation)
      → Task 005 (joins Chain A)
```

### Sequential Chain C: Work Item Infrastructure
```
Task 006 (WorkItemStatus enum)
  → Task 007 (WorkItem model)
    → Task 008 (IWorkItemStore interface)
      → Task 009 (InMemoryWorkItemStore)
        → Task 011 (joins Chain A)
          → Task 012 (DI registration)
```

### Parallel Groups

| Group | Tasks | Can Start After |
|-------|-------|-----------------|
| **Group 1** | 001, 006, 010 | Immediately (no dependencies) |
| **Group 2** | 002, 003, 007 | Group 1 complete |
| **Group 3** | 004, 008 | Group 2 complete |
| **Sequential** | 005, 009, 011, 012 | Respective dependencies |

```
Time →
─────────────────────────────────────────────────────────
Group 1:  [001: ServiceRequestInfo] [006: Enum] [010: RehydrateReq]
Group 2:  [002: ClinicalBundle] [003: IFhirClient] [007: WorkItem]
Group 3:  [004: FhirClient impl] [008: IWorkItemStore]
Seq:      [005: Aggregator] [009: InMemoryStore] [011: Endpoint] [012: DI]
```

## Completion Checklist
- [ ] All tests written before implementation
- [ ] All tests pass
- [ ] Code coverage meets standards
- [ ] ServiceRequest hydration integrated
- [ ] Work item state management functional
- [ ] Re-hydration endpoint operational
- [ ] Ready for review
