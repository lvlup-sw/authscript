# Implementation Plan: WorkItem CRUD Endpoints for Preview Testing

## Source Design
Link: `docs/designs/2026-01-31-workitem-crud-preview.md`

## Scope
**Gateway API (.NET)** — WorkItem CRUD endpoints, token handling, unit tests

## Summary
- Total tasks: 8
- Parallel groups: 2
- Estimated test count: ~18

## Task Breakdown

---

### Task 013: Create CreateWorkItemRequest Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `CreateWorkItemRequest_RequiredProperties_InitializesCorrectly`
   - File: `apps/gateway/Gateway.API.Tests/Models/CreateWorkItemRequestTests.cs`
   - Expected failure: Type `CreateWorkItemRequest` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `CreateWorkItemRequest_OptionalStatus_DefaultsToNull`
   - File: `apps/gateway/Gateway.API.Tests/Models/CreateWorkItemRequestTests.cs`
   - Expected failure: Property `Status` does not exist
   - Run: `dotnet test` - MUST FAIL

3. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/CreateWorkItemRequest.cs`
   - Changes: Create sealed record with required `EncounterId`, `PatientId`, `ServiceRequestId`, `ProcedureCode` and optional `Status`
   - Run: `dotnet test` - MUST PASS

4. [REFACTOR] Add XML documentation
   - Apply: Document all public members per coding standards
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 014: Create UpdateStatusRequest Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `UpdateStatusRequest_RequiredStatus_InitializesCorrectly`
   - File: `apps/gateway/Gateway.API.Tests/Models/UpdateStatusRequestTests.cs`
   - Expected failure: Type `UpdateStatusRequest` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/UpdateStatusRequest.cs`
   - Changes: Create sealed record with required `Status` property of type `WorkItemStatus`
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

### Task 015: Create WorkItemListResponse Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `WorkItemListResponse_RequiredProperties_InitializesCorrectly`
   - File: `apps/gateway/Gateway.API.Tests/Models/WorkItemListResponseTests.cs`
   - Expected failure: Type `WorkItemListResponse` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/WorkItemListResponse.cs`
   - Changes: Create sealed record with required `Items` (List<WorkItem>) and `Total` (int)
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

### Task 016: Add AccessToken to RehydrateRequest

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `RehydrateRequest_OptionalAccessToken_DefaultsToNull`
   - File: `apps/gateway/Gateway.API.Tests/Models/RehydrateRequestTests.cs`
   - Expected failure: Property `AccessToken` does not exist on `RehydrateRequest`
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Models/RehydrateRequest.cs`
   - Changes: Add optional `AccessToken` property with `string?` type
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Update XML documentation
   - Apply: Document the new property with usage context
   - Run: `dotnet test` - MUST STAY GREEN

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 017: Add GetAllAsync to IWorkItemStore and InMemoryWorkItemStore

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `IWorkItemStore_HasGetAllAsyncMethod`
   - File: `apps/gateway/Gateway.API.Tests/Contracts/IWorkItemStoreTests.cs`
   - Expected failure: Method `GetAllAsync` does not exist on interface
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `GetAllAsync_NoFilters_ReturnsAllItems`
   - File: `apps/gateway/Gateway.API.Tests/Services/InMemoryWorkItemStoreTests.cs`
   - Expected failure: Method not implemented
   - Run: `dotnet test` - MUST FAIL

3. [RED] Write test: `GetAllAsync_FilterByEncounterId_ReturnsMatching`
   - File: `apps/gateway/Gateway.API.Tests/Services/InMemoryWorkItemStoreTests.cs`
   - Expected failure: Filtering not implemented
   - Run: `dotnet test` - MUST FAIL

4. [RED] Write test: `GetAllAsync_FilterByStatus_ReturnsMatching`
   - File: `apps/gateway/Gateway.API.Tests/Services/InMemoryWorkItemStoreTests.cs`
   - Expected failure: Status filtering not implemented
   - Run: `dotnet test` - MUST FAIL

5. [GREEN] Implement minimum code
   - Files:
     - `apps/gateway/Gateway.API/Contracts/IWorkItemStore.cs` - Add method signature
     - `apps/gateway/Gateway.API/Services/InMemoryWorkItemStore.cs` - Implement with LINQ filtering
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 018: Implement CRUD Endpoint Methods

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `CreateAsync_ValidRequest_Returns201WithWorkItem`
   - File: `apps/gateway/Gateway.API.Tests/Endpoints/WorkItemEndpointsCrudTests.cs`
   - Expected failure: Method `CreateAsync` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `CreateAsync_WithStatus_UsesProvidedStatus`
   - Expected failure: Status not applied
   - Run: `dotnet test` - MUST FAIL

3. [RED] Write test: `ListAsync_NoFilters_ReturnsAllItems`
   - Expected failure: Method `ListAsync` does not exist
   - Run: `dotnet test` - MUST FAIL

4. [RED] Write test: `ListAsync_WithEncounterFilter_PassesToStore`
   - Expected failure: Filter not passed
   - Run: `dotnet test` - MUST FAIL

5. [RED] Write test: `GetByIdAsync_Exists_ReturnsWorkItem`
   - Expected failure: Method `GetByIdAsync` does not exist on endpoints
   - Run: `dotnet test` - MUST FAIL

6. [RED] Write test: `GetByIdAsync_NotExists_Returns404`
   - Expected failure: 404 not returned
   - Run: `dotnet test` - MUST FAIL

7. [RED] Write test: `UpdateStatusAsync_ValidUpdate_ReturnsUpdatedItem`
   - Expected failure: Method `UpdateStatusAsync` does not exist on endpoints
   - Run: `dotnet test` - MUST FAIL

8. [RED] Write test: `UpdateStatusAsync_NotExists_Returns404`
   - Expected failure: 404 not returned
   - Run: `dotnet test` - MUST FAIL

9. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Endpoints/WorkItemEndpoints.cs`
   - Changes: Add `CreateAsync`, `ListAsync`, `GetByIdAsync`, `UpdateStatusAsync` static methods
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 013, Task 014, Task 015, Task 017
**Parallelizable:** No (depends on model and store tasks)

---

### Task 019: Register CRUD Endpoints in MapWorkItemEndpoints

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `MapWorkItemEndpoints_RegistersAllCrudRoutes`
   - File: `apps/gateway/Gateway.API.Tests/Endpoints/WorkItemEndpointsCrudTests.cs`
   - Expected failure: Routes not registered
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Endpoints/WorkItemEndpoints.cs`
   - Changes: Update `MapWorkItemEndpoints` to register POST `/`, GET `/`, GET `/{id}`, PUT `/{id}/status`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 018
**Parallelizable:** No (depends on 018)

---

### Task 020: Update RehydrateAsync to Use AccessToken

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**
1. [RED] Write test: `RehydrateAsync_WithAccessToken_PassesTokenToAggregator`
   - File: `apps/gateway/Gateway.API.Tests/Endpoints/WorkItemEndpointsTests.cs`
   - Expected failure: Token not passed (still uses placeholder)
   - Run: `dotnet test` - MUST FAIL

2. [RED] Write test: `RehydrateAsync_WithoutAccessToken_UsesFallback`
   - Expected failure: Null token not handled
   - Run: `dotnet test` - MUST FAIL

3. [GREEN] Implement minimum code
   - File: `apps/gateway/Gateway.API/Endpoints/WorkItemEndpoints.cs`
   - Changes: Update `RehydrateAsync` to accept optional `RehydrateRequest` from body, use `request?.AccessToken ?? "placeholder-token"`
   - Run: `dotnet test` - MUST PASS

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** Task 016
**Parallelizable:** No (depends on 016)

---

## Parallelization Strategy

### Parallel Group 1: Foundation Models (No Dependencies)
```
Task 013 (CreateWorkItemRequest)  |  Task 014 (UpdateStatusRequest)  |  Task 015 (WorkItemListResponse)  |  Task 016 (RehydrateRequest)  |  Task 017 (GetAllAsync)
```

### Sequential Chain: Endpoints
```
Group 1 Complete
  → Task 018 (CRUD endpoint methods)
    → Task 019 (Route registration)
```

### Parallel after Task 016
```
Task 016 (RehydrateRequest update)
  → Task 020 (Use AccessToken in RehydrateAsync)
```

### Execution Graph

```
Time →
─────────────────────────────────────────────────────────────────────────
Group 1:  [013: CreateReq] [014: UpdateReq] [015: ListResp] [016: RehReq] [017: GetAll]
          ─────────────────────────────────────────────────────────────────
Seq A:                                    [018: CRUD Methods] → [019: Routes]
Seq B:                                    [020: Token handling]
```

## Integration Notes

### Existing Code Patterns
- Use `sealed record` for all models (per coding standards)
- Use `required` for mandatory properties
- Follow existing test patterns in `WorkItemEndpointsTests.cs`
- Use NSubstitute for mocking
- Use TUnit with awaited assertions

### Error Handling Convention
- Return `Results.NotFound(new ErrorResponse { ... })` for 404
- Use error code `"WORK_ITEM_NOT_FOUND"` for missing work items

### ID Generation
- Use `$"wi-{Guid.NewGuid():N}"` for work item IDs (consistent with existing pattern)

## Completion Checklist
- [ ] All tests written before implementation
- [ ] All tests pass
- [ ] POST /api/work-items creates work item, returns 201
- [ ] GET /api/work-items lists items with optional filters
- [ ] GET /api/work-items/{id} returns item or 404
- [ ] PUT /api/work-items/{id}/status updates status or 404
- [ ] POST /api/work-items/{id}/rehydrate accepts optional accessToken
- [ ] Ready for review
