# Implementation Plan: Patient Registry and Per-Patient Polling

## Source Design

- Design: `docs/designs/2026-01-29-athenahealth-pivot-mvp.md`
- Workflow: `docs/designs/2026-02-01-athenahealth-workflow.md`

## Summary

- Total tasks: 24
- Parallel groups: 3
- Estimated test count: ~65

## Gap Analysis

The workflow document describes a **SMART on FHIR embedded app** architecture where:

1. **Provider opens AuthScript** from patient chart → SMART launch provides patient context
2. **Dashboard registers patient** with Gateway → `POST /api/patients/register`
3. **Gateway creates work item** in `PENDING` status
4. **Gateway polls per-patient** → Query: `patient={id}&_id={encounterId}&ah-practice=Organization/a-1.Practice-{practiceId}`
5. **Encounter completion triggers** PA workflow → Status transition to "finished"
6. **Unregister patient** from active polling → Move to processing queue
7. **Hydrate + Analyze** → Update work item status
8. **SSE notification** → Dashboard shows result

### Current State vs. Required State

| Component | Current | Required |
| --- | --- | --- |
| Polling model | Global (all finished encounters) | Per-patient (registered patients only) |
| Patient registry | None | `IPatientRegistry` + `InMemoryPatientRegistry` |
| Registration endpoint | None | `POST /api/patients/register` |
| WorkItemStatus | 5 states | 6 states (add `Pending`) |
| Encounter detection | `status=finished&date=gt{X}` | Status transition detection per patient |
| Work item creation | Manual via API | Automatic on patient registration |
| Auto-unregister | None | After encounter completion |
| SSE integration | Exists | Wire to new flow |

---

## Task Breakdown

### Phase 1: Models and Contracts (Parallelizable)

---

### Task 001: Create RegisteredPatient Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `RegisteredPatient_AllProperties_CanBeInitialized`
   - File: `apps/gateway/Gateway.API.Tests/Models/RegisteredPatientTests.cs`
   - Expected failure: Type `RegisteredPatient` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement model
   - File: `apps/gateway/Gateway.API/Models/RegisteredPatient.cs`
   - Properties:
     - `PatientId` (required string)
     - `EncounterId` (required string)
     - `PracticeId` (required string)
     - `WorkItemId` (required string) - NEW: Links to work item
     - `RegisteredAt` (required DateTimeOffset)
     - `LastPolledAt` (DateTimeOffset?)
     - `CurrentEncounterStatus` (string?)
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Add XML documentation

**Verification:**
- [ ] Witnessed test fail for the right reason
- [ ] Test passes after implementation
- [ ] No extra code beyond test requirements

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 002: Create RegisterPatientRequest Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `RegisterPatientRequest_RequiredProperties_AreEnforced`
   - File: `apps/gateway/Gateway.API.Tests/Models/RegisterPatientRequestTests.cs`
   - Expected failure: Type `RegisterPatientRequest` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement model
   - File: `apps/gateway/Gateway.API/Models/RegisterPatientRequest.cs`
   - Properties:
     - `PatientId` (required string)
     - `EncounterId` (required string)
     - `PracticeId` (required string)
   - Note: ProcedureCode NOT included - determined during hydration/analysis
   - Run: `dotnet test` - MUST PASS

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 003: Add Pending Status to WorkItemStatus Enum

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `WorkItemStatus_Pending_Exists`
   - File: `apps/gateway/Gateway.API.Tests/Models/WorkItemStatusTests.cs`
   - Expected failure: `WorkItemStatus.Pending` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Add Pending to enum
   - File: `apps/gateway/Gateway.API/Models/WorkItemStatus.cs`
   - Add `Pending` value (as first value = 0, shift others)
   - XML doc: "Patient registered, awaiting encounter completion"
   - Run: `dotnet test` - MUST PASS

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 003A: Make WorkItem ServiceRequestId and ProcedureCode Optional

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `WorkItem_CanBeCreatedWithoutServiceRequestInfo`
   - File: `apps/gateway/Gateway.API.Tests/Models/WorkItemTests.cs`
   - Create WorkItem with null ServiceRequestId and ProcedureCode
   - Expected failure: `required` modifier prevents null assignment
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Update WorkItem model
   - File: `apps/gateway/Gateway.API/Models/WorkItem.cs`
   - Change `ServiceRequestId` from `required string` to `string?`
   - Change `ProcedureCode` from `required string` to `string?`
   - Update XML docs to indicate these are populated after analysis
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Update CreateWorkItemRequest
   - File: `apps/gateway/Gateway.API/Models/CreateWorkItemRequest.cs`
   - Make `ServiceRequestId` and `ProcedureCode` optional
   - Run: `dotnet test` - MUST PASS

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 004: Create EncounterCompletedEvent Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `EncounterCompletedEvent_AllProperties_CanBeInitialized`
   - File: `apps/gateway/Gateway.API.Tests/Models/EncounterCompletedEventTests.cs`
   - Expected failure: Type does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement model
   - File: `apps/gateway/Gateway.API/Models/EncounterCompletedEvent.cs`
   - Properties:
     - `PatientId` (required string)
     - `EncounterId` (required string)
     - `PracticeId` (required string)
     - `WorkItemId` (required string)
   - Run: `dotnet test` - MUST PASS

**Dependencies:** None
**Parallelizable:** Yes

---

### Task 005: Create RegisterPatientResponse Model

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `RegisterPatientResponse_ContainsWorkItemId`
   - File: `apps/gateway/Gateway.API.Tests/Models/RegisterPatientResponseTests.cs`
   - Expected failure: Type does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement model
   - File: `apps/gateway/Gateway.API/Models/RegisterPatientResponse.cs`
   - Properties:
     - `WorkItemId` (required string)
     - `Message` (required string)
   - Run: `dotnet test` - MUST PASS

**Dependencies:** None
**Parallelizable:** Yes

---

### Phase 2: Patient Registry Infrastructure (Sequential)

---

### Task 006: Create IPatientRegistry Interface

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `IPatientRegistry_InterfaceExists_WithExpectedMethods`
   - File: `apps/gateway/Gateway.API.Tests/Contracts/IPatientRegistryTests.cs`
   - Expected failure: Type `IPatientRegistry` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement interface
   - File: `apps/gateway/Gateway.API/Contracts/IPatientRegistry.cs`
   - Methods:
     - `RegisterAsync(RegisteredPatient patient, CancellationToken ct = default)` → `Task`
     - `GetActiveAsync(CancellationToken ct = default)` → `Task<IReadOnlyList<RegisteredPatient>>`
     - `UnregisterAsync(string patientId, CancellationToken ct = default)` → `Task`
     - `GetAsync(string patientId, CancellationToken ct = default)` → `Task<RegisteredPatient?>`
     - `UpdateAsync(string patientId, DateTimeOffset lastPolled, string status, CancellationToken ct = default)` → `Task<bool>`
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 001 (RegisteredPatient model)
**Parallelizable:** No (starts chain)

---

### Task 007: Implement InMemoryPatientRegistry - RegisterAsync

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `RegisterAsync_ValidPatient_AddsToRegistry`
   - File: `apps/gateway/Gateway.API.Tests/Services/InMemoryPatientRegistryTests.cs`
   - Expected failure: Type `InMemoryPatientRegistry` does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement RegisterAsync
   - File: `apps/gateway/Gateway.API/Services/InMemoryPatientRegistry.cs`
   - Use `ConcurrentDictionary<string, RegisteredPatient>`
   - Key on PatientId
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 006
**Parallelizable:** No

---

### Task 008: Implement InMemoryPatientRegistry - GetAsync and GetActiveAsync

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `GetAsync_ExistingPatient_ReturnsPatient`
   - `GetAsync_NonExistentPatient_ReturnsNull`
   - `GetActiveAsync_ReturnsAllActive_FiltersExpired`
   - `GetActiveAsync_RespectsExpirationTime` (12 hours)
   - File: `apps/gateway/Gateway.API.Tests/Services/InMemoryPatientRegistryTests.cs`
   - Expected failure: Methods not implemented
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement methods
   - `_expirationTime = TimeSpan.FromHours(12)`
   - Filter patients where `RegisteredAt > cutoff`
   - Return as `IReadOnlyList<RegisteredPatient>`
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 007
**Parallelizable:** No

---

### Task 009: Implement InMemoryPatientRegistry - UnregisterAsync and UpdateAsync

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `UnregisterAsync_ExistingPatient_RemovesFromRegistry`
   - `UnregisterAsync_NonExistentPatient_DoesNotThrow`
   - `UpdateAsync_ExistingPatient_UpdatesFields`
   - `UpdateAsync_NonExistentPatient_ReturnsFalse`
   - File: `apps/gateway/Gateway.API.Tests/Services/InMemoryPatientRegistryTests.cs`
   - Expected failure: Methods not implemented
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement methods
   - UnregisterAsync: TryRemove from dictionary
   - UpdateAsync: Get, create new with `with` expression, TryUpdate
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 008
**Parallelizable:** No

---

### Phase 3: Patient Endpoints (Sequential, depends on Phase 2)

---

### Task 010: Create PatientEndpoints - Register Endpoint with Work Item Creation

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `RegisterAsync_ValidRequest_CreatesWorkItemInPendingStatus`
   - `RegisterAsync_ValidRequest_RegistersPatientWithWorkItemId`
   - `RegisterAsync_ValidRequest_ReturnsWorkItemId`
   - File: `apps/gateway/Gateway.API.Tests/Endpoints/PatientEndpointsTests.cs`
   - Expected failure: Type/method does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement RegisterAsync
   - File: `apps/gateway/Gateway.API/Endpoints/PatientEndpoints.cs`
   - Route: `POST /api/patients/register`
   - Steps:
     1. Create `WorkItem` with:
        - `Status = Pending`
        - `PatientId`, `EncounterId` from request
        - `ServiceRequestId = null`, `ProcedureCode = null` (set after analysis)
     2. Save via `IWorkItemStore.CreateAsync()`
     3. Create `RegisteredPatient` with `WorkItemId`
     4. Save via `IPatientRegistry.RegisterAsync()`
     5. Return `RegisterPatientResponse` with WorkItemId
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 009 (InMemoryPatientRegistry complete), Task 003A (optional WorkItem fields)
**Parallelizable:** No

---

### Task 011: Create PatientEndpoints - Unregister and Get Endpoints

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `UnregisterAsync_ExistingPatient_Returns200`
   - `UnregisterAsync_NonExistentPatient_Returns200` (idempotent)
   - `GetAsync_ExistingPatient_ReturnsPatient`
   - `GetAsync_NonExistentPatient_Returns404`
   - File: `apps/gateway/Gateway.API.Tests/Endpoints/PatientEndpointsTests.cs`
   - Expected failure: Methods do not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement endpoints
   - `DELETE /api/patients/{patientId}` - Unregister
   - `GET /api/patients/{patientId}` - Get status
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 010
**Parallelizable:** No

---

### Task 012: Register PatientEndpoints and IPatientRegistry in DI

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `AddGatewayServices_RegistersPatientRegistry`
   - File: `apps/gateway/Gateway.API.Tests/DependencyExtensionsTests.cs`
   - Expected failure: Service not registered
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Register services
   - File: `apps/gateway/Gateway.API/DependencyExtensions.cs`
   - Add: `services.AddScoped<IPatientRegistry, PostgresPatientRegistry>();`
     (Note: Original plan specified `InMemoryPatientRegistry` as interim; now superseded by `PostgresPatientRegistry`)
   - File: `apps/gateway/Gateway.API/Program.cs`
   - Add: `app.MapPatientEndpoints();`
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 011
**Parallelizable:** No

---

### Phase 4: Polling Service Refactor (Sequential, depends on Phase 2)

---

### Task 013: Create AthenaQueryBuilder for Proper Query Formatting

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `BuildEncounterQuery_FormatsAhPracticeCorrectly`
   - `BuildEncounterQuery_IncludesAllRequiredParameters`
   - File: `apps/gateway/Gateway.API.Tests/Services/AthenaQueryBuilderTests.cs`
   - Expected failure: Type does not exist
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement query builder
   - File: `apps/gateway/Gateway.API/Services/AthenaQueryBuilder.cs`
   - Method: `BuildEncounterQuery(string patientId, string encounterId, string practiceId)`
   - Format: `patient={patientId}&_id={encounterId}&ah-practice=Organization/a-1.Practice-{practiceId}`
   - Run: `dotnet test` - MUST PASS

**Dependencies:** None
**Parallelizable:** Yes (parallel to Phase 3)

---

### Task 014: Refactor AthenaPollingService - Add IPatientRegistry Dependency

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `Constructor_RequiresPatientRegistry`
   - `ExecuteAsync_GetsActivePatients_FromRegistry`
   - File: `apps/gateway/Gateway.API.Tests/Services/Polling/AthenaPollingServiceTests.cs`
   - Expected failure: Constructor doesn't accept IPatientRegistry
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Add dependency
   - Inject `IPatientRegistry` in constructor
   - Store as `_registry` field
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 009 (InMemoryPatientRegistry complete)
**Parallelizable:** Yes (parallel to Phase 3)

---

### Task 015: Refactor AthenaPollingService - Per-Patient Polling Loop

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `ExecuteAsync_NoRegisteredPatients_DoesNotQueryFhir`
   - `ExecuteAsync_WithRegisteredPatients_PollsEachPatient`
   - `ExecuteAsync_UsesParallelForEachAsync_WithMaxDegreeOf5`
   - File: `apps/gateway/Gateway.API.Tests/Services/Polling/AthenaPollingServiceTests.cs`
   - Expected failure: Still polls globally
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Refactor ExecuteAsync
   - Replace global polling with:
     ```csharp
     var patients = await _registry.GetActiveAsync(ct);
     await Parallel.ForEachAsync(
         patients,
         new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct },
         async (patient, ct) => await PollPatientEncounterAsync(patient, ct));
     ```
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Remove old global polling code

**Dependencies:** Task 014
**Parallelizable:** No

---

### Task 016: Implement PollPatientEncounterAsync with Status Transition Detection

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `PollPatientEncounterAsync_EncounterInProgress_UpdatesRegistryOnly`
   - `PollPatientEncounterAsync_EncounterFinished_EmitsEventAndUnregisters`
   - `PollPatientEncounterAsync_AlreadyFinished_EmitsEventImmediately`
   - `PollPatientEncounterAsync_FhirError_LogsAndContinues`
   - File: `apps/gateway/Gateway.API.Tests/Services/Polling/AthenaPollingServiceTests.cs`
   - Expected failure: Method not implemented
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Implement PollPatientEncounterAsync
   - Use `AthenaQueryBuilder.BuildEncounterQuery()`
   - Extract status from FHIR response
   - If status == "finished" AND CurrentEncounterStatus != "finished":
     1. Emit `EncounterCompletedEvent` to channel
     2. Call `_registry.UnregisterAsync(patientId)` - Auto-unregister
   - Else: Call `_registry.UpdateAsync(patientId, now, status)`
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 015, Task 013
**Parallelizable:** No

---

### Task 017: Handle Already-Finished Encounters on Registration

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `PollPatientEncounterAsync_InitialPollFinished_EmitsImmediately`
   - File: `apps/gateway/Gateway.API.Tests/Services/Polling/AthenaPollingServiceTests.cs`
   - Scenario: Patient registered, first poll shows "finished"
   - Expected: Event emitted immediately
   - Expected failure: Only triggers on transition, not initial state
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Update logic
   - If `CurrentEncounterStatus` is null (first poll) AND status == "finished":
     - Emit event immediately (treat as completed)
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 016
**Parallelizable:** No

---

### Phase 5: EncounterProcessor Integration (Sequential, depends on Phase 4)

---

### Task 018: Update Channel Type to EncounterCompletedEvent

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `EncounterChannel_AcceptsEncounterCompletedEvent`
   - File: `apps/gateway/Gateway.API.Tests/Services/Polling/AthenaPollingServiceTests.cs`
   - Expected failure: Channel type mismatch
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Update channel
   - Change `Channel<string>` to `Channel<EncounterCompletedEvent>`
   - Update `Encounters` property type
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 017
**Parallelizable:** No

---

### Task 019: Update EncounterProcessor to Consume EncounterCompletedEvent

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `ProcessAsync_ReceivesEvent_HydratesWithCorrectPatientId`
   - `ProcessAsync_ReceivesEvent_UpdatesWorkItemStatus`
   - `ProcessAsync_AnalysisComplete_UpdatesWorkItemToReadyForReview`
   - `ProcessAsync_AnalysisIncomplete_UpdatesWorkItemToMissingData`
   - File: `apps/gateway/Gateway.API.Tests/Services/EncounterProcessorTests.cs`
   - Expected failure: Processor doesn't accept new event type
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Update EncounterProcessor
   - Change signature to accept `EncounterCompletedEvent`
   - Use `event.PatientId` for hydration
   - Use `event.WorkItemId` to update status
   - Call `IWorkItemStore.UpdateStatusAsync()` with result
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 018
**Parallelizable:** No

---

### Task 019A: Update WorkItem with ServiceRequest Info After Analysis

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `ProcessAsync_AnalysisComplete_SetsServiceRequestId`
   - `ProcessAsync_AnalysisComplete_SetsProcedureCode`
   - `ProcessAsync_NoPaRequired_SetsServiceRequestIdFromFirstOrder`
   - File: `apps/gateway/Gateway.API.Tests/Services/EncounterProcessorTests.cs`
   - Expected failure: ServiceRequestId/ProcedureCode not updated
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Update EncounterProcessor
   - After analysis, extract ServiceRequestId and ProcedureCode from result
   - Add `IWorkItemStore.UpdateAsync()` method to update arbitrary fields
   - Update work item with ServiceRequestId, ProcedureCode from analysis
   - Run: `dotnet test` - MUST PASS

3. [REFACTOR] Add UpdateAsync to IWorkItemStore interface
   - File: `apps/gateway/Gateway.API/Contracts/IWorkItemStore.cs`
   - Method: `Task<bool> UpdateAsync(string id, WorkItem updated, CancellationToken ct)`
   - Implement in InMemoryWorkItemStore

**Dependencies:** Task 019
**Parallelizable:** No

---

### Task 020: Wire SSE Notification on Work Item Status Change

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write tests:
   - `ProcessAsync_StatusUpdatedToReadyForReview_SendsSseNotification`
   - `ProcessAsync_StatusUpdatedToMissingData_SendsSseNotification`
   - `ProcessAsync_StatusUpdatedToNoPaRequired_SendsSseNotification`
   - File: `apps/gateway/Gateway.API.Tests/Services/EncounterProcessorTests.cs`
   - Expected failure: No SSE notification sent
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Add SSE notification
   - After status update, call `INotificationHub.WriteAsync()` with:
     - `Type = "WORK_ITEM_STATUS_CHANGED"`
     - `WorkItemId`
     - `NewStatus`
     - `PatientId`
     - `ServiceRequestId` (if available)
     - `ProcedureCode` (if available)
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 019A
**Parallelizable:** No

---

### Phase 6: Integration Tests (After all phases)

---

### Task 021: Alba Integration Test - Patient Registration Flow

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `PatientRegistration_CreatesWorkItemAndReturnsId`
   - File: `apps/gateway/Gateway.API.Tests/Integration/PatientEndpointsIntegrationTests.cs`
   - Test full HTTP flow with Alba
   - Expected failure: Integration not wired
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Verify integration
   - POST `/api/patients/register` with valid request
   - Assert response contains `workItemId`
   - GET `/api/work-items/{id}` returns work item in `Pending` status
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Tasks 001-020
**Parallelizable:** No

---

### Task 022: Alba Integration Test - Full Encounter Completion Flow

**Phase:** RED → GREEN → REFACTOR

**TDD Steps:**

1. [RED] Write test: `EncounterCompletion_UpdatesWorkItemAndSendsNotification`
   - File: `apps/gateway/Gateway.API.Tests/Integration/EncounterProcessingIntegrationTests.cs`
   - Test end-to-end with mocked FHIR responses
   - Expected failure: Full flow not integrated
   - Run: `dotnet test` - MUST FAIL

2. [GREEN] Verify full flow
   - Register patient → Work item in `Pending`
   - Simulate encounter completion via polling
   - Assert work item status updated
   - Assert SSE notification sent
   - Run: `dotnet test` - MUST PASS

**Dependencies:** Task 021
**Parallelizable:** No

---

## Parallelization Strategy

```text
                   ┌─────────────────────────────────────────────────────────────────┐
                   │                    PARALLEL GROUP 1                              │
                   │               (Models - No Dependencies)                         │
                   ├─────────────────────────────────────────────────────────────────┤
                   │ Task 001: RegisteredPatient Model                                │
                   │ Task 002: RegisterPatientRequest Model                           │
                   │ Task 003: WorkItemStatus Pending                                 │
                   │ Task 003A: WorkItem Optional Fields                              │
                   │ Task 004: EncounterCompletedEvent Model                          │
                   │ Task 005: RegisterPatientResponse Model                          │
                   └─────────────────────────────────────────────────────────────────┘
                                              │
                                              ▼
                   ┌─────────────────────────────────────────────────────────────────┐
                   │              SEQUENTIAL: Registry Implementation                 │
                   │                    (Tasks 006-009)                               │
                   └─────────────────────────────────────────────────────────────────┘
                                              │
              ┌───────────────────────────────┴───────────────────────────────┐
              │                                                               │
              ▼                                                               ▼
┌─────────────────────────────────────┐         ┌─────────────────────────────────────┐
│        PARALLEL GROUP 2             │         │        PARALLEL GROUP 3             │
│     (Patient Endpoints)             │         │     (Polling Refactor)              │
├─────────────────────────────────────┤         ├─────────────────────────────────────┤
│ Task 010: Register Endpoint         │         │ Task 013: AthenaQueryBuilder        │
│ Task 011: Unregister/Get Endpoints  │         │ Task 014: Add Registry Dependency   │
│ Task 012: DI Registration           │         │ Task 015: Per-Patient Loop          │
│                                     │         │ Task 016: Status Transition         │
│                                     │         │ Task 017: Already-Finished Handler  │
└─────────────────────────────────────┘         └─────────────────────────────────────┘
              │                                               │
              └───────────────────────────────┬───────────────┘
                                              │
                                              ▼
                   ┌─────────────────────────────────────────────────────────────────┐
                   │              SEQUENTIAL: EncounterProcessor Integration          │
                   │                    (Tasks 018-020)                               │
                   └─────────────────────────────────────────────────────────────────┘
                                              │
                                              ▼
                   ┌─────────────────────────────────────────────────────────────────┐
                   │              SEQUENTIAL: Integration Tests                       │
                   │                    (Tasks 021-022)                               │
                   └─────────────────────────────────────────────────────────────────┘
```

### Execution Graph

```text
Time →

Worktree 1:  [001]──┐
Worktree 2:  [002]──┤
Worktree 3:  [003]──┼──[006]──[007]──[008]──[009]──┬──[010]──[011]──[012]──┐
Worktree 4:  [004]──┤                              │                       │
Worktree 5:  [005]──┘                              │                       │
                                                   │                       ├──[018]──[019]──[020]──[021]──[022]
                                                   └──[013]──[014]──[015]──[016]──[017]──┘
```

---

## Integration Points

### Existing Code Changes Required

| File | Change Type | Description |
| ---- | ----------- | ----------- |
| `WorkItemStatus.cs` | Modify | Add `Pending` enum value |
| `WorkItem.cs` | Modify | Make `ServiceRequestId` and `ProcedureCode` optional |
| `CreateWorkItemRequest.cs` | Modify | Make `ServiceRequestId` and `ProcedureCode` optional |
| `IWorkItemStore.cs` | Modify | Add `UpdateAsync` method for full work item updates |
| `InMemoryWorkItemStore.cs` | Modify | Implement `UpdateAsync` |
| `AthenaPollingService.cs` | Refactor | Per-patient polling, inject IPatientRegistry |
| `EncounterProcessor.cs` | Modify | Accept EncounterCompletedEvent, update work item with service request info |
| `DependencyExtensions.cs` | Modify | Register `IPatientRegistry` |
| `Program.cs` | Modify | Map patient endpoints |

### New Files

| File | Purpose |
| ---- | ------- |
| `Models/RegisteredPatient.cs` | Patient registration model |
| `Models/RegisterPatientRequest.cs` | API request model |
| `Models/RegisterPatientResponse.cs` | API response with WorkItemId |
| `Models/EncounterCompletedEvent.cs` | Event for encounter completion |
| `Contracts/IPatientRegistry.cs` | Registry interface |
| `Services/PostgresPatientRegistry.cs` | PostgreSQL-backed implementation (supersedes InMemoryPatientRegistry) |
| `Services/AthenaQueryBuilder.cs` | Query formatting helper |
| `Endpoints/PatientEndpoints.cs` | Patient registration endpoints |

---

## Key Design Decisions

### 1. Work Item Creation on Registration

Per workflow Phase 1, Step 6:
> "POST `/api/patients/register` with patientId, encounterId, practiceId"
> "Gateway adds patient to monitoring queue"

The plan adds automatic work item creation in `Pending` status when a patient is registered. This matches the state machine:
```
[*] --> PENDING: Patient registered
```

### 2. Automatic Unregistration

Per workflow Phase 2, Step 3:
> "Unregister patient from active polling (move to processing queue)"

The plan includes automatic unregistration in `PollPatientEncounterAsync` when encounter status transitions to "finished".

### 3. ah-practice Query Parameter Format

Per athenahealth API constraints:
```
ah-practice=Organization/a-1.Practice-{practiceId}
```

Task 013 creates `AthenaQueryBuilder` to ensure correct formatting.

### 4. Already-Finished Encounter Handling

Task 017 handles the edge case where a provider registers after the encounter is already finished. The first poll detects "finished" status and immediately emits the event.

### 5. SSE Notification Integration

Task 020 wires SSE notifications for work item status changes, ensuring Dashboard receives real-time updates.

### 6. WorkItem Optional Fields

Per user decision, `ServiceRequestId` and `ProcedureCode` are made optional (nullable):
- Work item created at registration with `PENDING` status and null service request info
- After hydration and analysis (Phase 3-4), work item updated with actual ServiceRequestId and ProcedureCode
- This matches the workflow where procedure codes aren't known until clinical data is fetched

---

## Completion Checklist

- [ ] All tests written before implementation
- [ ] All 24 tasks completed (001-005, 003A, 006-022, 019A)
- [ ] All tests pass
- [ ] Code coverage meets standards
- [ ] DI registration complete
- [ ] Endpoints mapped in Program.cs
- [ ] Integration tests pass (Tasks 021-022)
- [ ] WorkItem optional fields migration complete
- [ ] Ready for review

---

## Notes

### Backward Compatibility

The `Pending` status is inserted as value 0:
- Existing code expecting `ReadyForReview = 0` needs updating
- JSON serialization uses string names (via JsonStringEnumConverter), so wire format unchanged
- Existing work items in DB would need migration (not MVP concern)

### Testing Strategy

1. **Unit tests**: Each task includes specific test cases (~50 tests)
2. **Integration tests**: Tasks 021-022 with Alba (~10 tests)
3. **Smoke test**: Manual test with athenahealth sandbox after implementation
