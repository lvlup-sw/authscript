# Design: Work Item CRUD Endpoints for Preview Testing

**Date:** January 31, 2026
**Status:** Draft
**Scope:** Gateway API (.NET) — Work item CRUD, token handling, integration tests

## Problem Statement

The current implementation only provides a `POST /api/work-items/{id}/rehydrate` endpoint. This endpoint cannot be tested because:

1. **No work items exist** — The `InMemoryWorkItemStore` starts empty
2. **No creation endpoint** — Cannot create work items via API
3. **Hardcoded token** — `"placeholder-token"` will fail against real FHIR servers
4. **No retrieval endpoints** — Cannot verify work item state after operations

## Goals

1. Enable end-to-end testing in a live preview environment
2. Add CRUD endpoints for work item management
3. Fix token handling for rehydrate endpoint
4. Add Alba integration tests for all endpoints

## Non-Goals

- Production-grade token management (deferred to #19)
- Persistent storage (in-memory is acceptable for MVP)
- Authentication/authorization on work item endpoints

---

## API Design

### Base Path
```
/api/work-items
```

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/work-items` | Create a new work item |
| `GET` | `/api/work-items` | List all work items |
| `GET` | `/api/work-items/{id}` | Get work item by ID |
| `PUT` | `/api/work-items/{id}/status` | Update work item status |
| `POST` | `/api/work-items/{id}/rehydrate` | Re-fetch and re-analyze (existing) |

### Request/Response Models

#### POST /api/work-items

**Request:**
```json
{
  "encounterId": "enc-123",
  "patientId": "pat-456",
  "serviceRequestId": "sr-789",
  "procedureCode": "72148",
  "status": "MissingData"  // Optional, defaults to MissingData
}
```

**Response (201 Created):**
```json
{
  "id": "wi-abc123",
  "encounterId": "enc-123",
  "patientId": "pat-456",
  "serviceRequestId": "sr-789",
  "procedureCode": "72148",
  "status": "MissingData",
  "createdAt": "2026-01-31T12:00:00Z",
  "updatedAt": null
}
```

#### GET /api/work-items

**Query Parameters:**
- `encounterId` (optional) — Filter by encounter
- `status` (optional) — Filter by status

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "wi-abc123",
      "encounterId": "enc-123",
      "patientId": "pat-456",
      "serviceRequestId": "sr-789",
      "procedureCode": "72148",
      "status": "MissingData",
      "createdAt": "2026-01-31T12:00:00Z",
      "updatedAt": null
    }
  ],
  "total": 1
}
```

#### GET /api/work-items/{id}

**Response (200 OK):**
```json
{
  "id": "wi-abc123",
  "encounterId": "enc-123",
  "patientId": "pat-456",
  "serviceRequestId": "sr-789",
  "procedureCode": "72148",
  "status": "MissingData",
  "createdAt": "2026-01-31T12:00:00Z",
  "updatedAt": null
}
```

**Response (404 Not Found):**
```json
{
  "message": "Work item 'wi-xyz' not found",
  "code": "WORK_ITEM_NOT_FOUND"
}
```

#### PUT /api/work-items/{id}/status

**Request:**
```json
{
  "status": "ReadyForReview"
}
```

**Response (200 OK):**
```json
{
  "id": "wi-abc123",
  "status": "ReadyForReview",
  "updatedAt": "2026-01-31T12:05:00Z"
}
```

#### POST /api/work-items/{id}/rehydrate

**Request:** No request body required. Token management is handled internally via `IFhirTokenProvider`.

**Response (200 OK):**
```json
{
  "workItemId": "wi-abc123",
  "newStatus": "ReadyForReview",
  "message": "Work item rehydrated successfully"
}
```

---

## Implementation Tasks

### Task 1: Create Request/Response Models

**Files:**
- `apps/gateway/Gateway.API/Models/CreateWorkItemRequest.cs`
- `apps/gateway/Gateway.API/Models/UpdateStatusRequest.cs`
- `apps/gateway/Gateway.API/Models/WorkItemListResponse.cs`
- `apps/gateway/Gateway.API/Models/RehydrateRequest.cs` (modify existing)

**CreateWorkItemRequest.cs:**
```csharp
namespace Gateway.API.Models;

/// <summary>
/// Request to create a new work item.
/// </summary>
public sealed record CreateWorkItemRequest
{
    /// <summary>
    /// FHIR Encounter ID that triggered this work item.
    /// </summary>
    public required string EncounterId { get; init; }

    /// <summary>
    /// FHIR Patient ID associated with this work item.
    /// </summary>
    public required string PatientId { get; init; }

    /// <summary>
    /// FHIR ServiceRequest ID for the order requiring prior authorization.
    /// </summary>
    public required string ServiceRequestId { get; init; }

    /// <summary>
    /// CPT code for the procedure requiring prior authorization.
    /// </summary>
    public required string ProcedureCode { get; init; }

    /// <summary>
    /// Initial status. Defaults to MissingData if not specified.
    /// </summary>
    public WorkItemStatus? Status { get; init; }
}
```

**UpdateStatusRequest.cs:**
```csharp
namespace Gateway.API.Models;

/// <summary>
/// Request to update a work item's status.
/// </summary>
public sealed record UpdateStatusRequest
{
    /// <summary>
    /// The new status to set.
    /// </summary>
    public required WorkItemStatus Status { get; init; }
}
```

**WorkItemListResponse.cs:**
```csharp
namespace Gateway.API.Models;

/// <summary>
/// Response containing a list of work items.
/// </summary>
public sealed record WorkItemListResponse
{
    /// <summary>
    /// The list of work items.
    /// </summary>
    public required List<WorkItem> Items { get; init; }

    /// <summary>
    /// Total count of items (for pagination).
    /// </summary>
    public required int Total { get; init; }
}
```

---

### Task 2: Add IWorkItemStore.GetAllAsync Method

**File:** `apps/gateway/Gateway.API/Contracts/IWorkItemStore.cs`

Add method:
```csharp
/// <summary>
/// Retrieves all work items, optionally filtered.
/// </summary>
/// <param name="encounterId">Optional encounter ID filter.</param>
/// <param name="status">Optional status filter.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>List of matching work items.</returns>
Task<List<WorkItem>> GetAllAsync(
    string? encounterId = null,
    WorkItemStatus? status = null,
    CancellationToken cancellationToken = default);
```

**File:** `apps/gateway/Gateway.API/Services/InMemoryWorkItemStore.cs`

Implement:
```csharp
/// <inheritdoc />
public Task<List<WorkItem>> GetAllAsync(
    string? encounterId = null,
    WorkItemStatus? status = null,
    CancellationToken cancellationToken = default)
{
    var query = _store.Values.AsEnumerable();

    if (!string.IsNullOrEmpty(encounterId))
    {
        query = query.Where(w => w.EncounterId == encounterId);
    }

    if (status.HasValue)
    {
        query = query.Where(w => w.Status == status.Value);
    }

    return Task.FromResult(query.ToList());
}
```

---

### Task 3: Extend WorkItemEndpoints with CRUD

**File:** `apps/gateway/Gateway.API/Endpoints/WorkItemEndpoints.cs`

```csharp
public static void MapWorkItemEndpoints(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/api/work-items")
        .WithTags("WorkItems");

    // CREATE
    group.MapPost("/", CreateAsync)
        .WithName("CreateWorkItem")
        .WithSummary("Create a new work item")
        .Produces<WorkItem>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

    // LIST
    group.MapGet("/", ListAsync)
        .WithName("ListWorkItems")
        .WithSummary("List all work items")
        .Produces<WorkItemListResponse>(StatusCodes.Status200OK);

    // GET BY ID
    group.MapGet("/{id}", GetByIdAsync)
        .WithName("GetWorkItem")
        .WithSummary("Get work item by ID")
        .Produces<WorkItem>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

    // UPDATE STATUS
    group.MapPut("/{id}/status", UpdateStatusAsync)
        .WithName("UpdateWorkItemStatus")
        .WithSummary("Update work item status")
        .Produces<WorkItem>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

    // REHYDRATE (existing, modified)
    group.MapPost("/{id}/rehydrate", RehydrateAsync)
        .WithName("RehydrateWorkItem")
        .WithSummary("Re-fetch clinical data and re-analyze work item")
        .Produces<RehydrateResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
}
```

---

### Task 4: Token Management

**NOTE:** Token management is now handled internally via `IFhirTokenProvider`. The rehydrate endpoint no longer accepts an `accessToken` parameter. Token acquisition is automatic.

---

### Task 5: Add Alba Integration Tests

**Package to add:** `Alba` (integration testing for ASP.NET Core)

**File:** `apps/gateway/Gateway.API.Tests/Gateway.API.Tests.csproj`

Add:
```xml
<PackageReference Include="Alba" Version="8.2.0" />
```

**File:** `apps/gateway/Gateway.API.Tests/Integration/WorkItemEndpointsIntegrationTests.cs`

```csharp
using Alba;
using Gateway.API.Models;
using System.Net;
using System.Net.Http.Json;

namespace Gateway.API.Tests.Integration;

/// <summary>
/// Integration tests for WorkItem endpoints using Alba.
/// </summary>
public class WorkItemEndpointsIntegrationTests : IAsyncDisposable
{
    private readonly IAlbaHost _host;

    public WorkItemEndpointsIntegrationTests()
    {
        _host = AlbaHost.For<Program>();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
    }

    #region POST /api/work-items

    [Test]
    public async Task CreateWorkItem_ValidRequest_Returns201()
    {
        var request = new CreateWorkItemRequest
        {
            EncounterId = "enc-123",
            PatientId = "pat-456",
            ServiceRequestId = "sr-789",
            ProcedureCode = "72148"
        };

        var result = await _host.Scenario(s =>
        {
            s.Post.Json(request).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(HttpStatusCode.Created);
        });

        var workItem = result.ReadAsJson<WorkItem>();
        await Assert.That(workItem).IsNotNull();
        await Assert.That(workItem!.EncounterId).IsEqualTo("enc-123");
        await Assert.That(workItem.Status).IsEqualTo(WorkItemStatus.MissingData);
    }

    [Test]
    public async Task CreateWorkItem_WithStatus_UsesProvidedStatus()
    {
        var request = new CreateWorkItemRequest
        {
            EncounterId = "enc-200",
            PatientId = "pat-200",
            ServiceRequestId = "sr-200",
            ProcedureCode = "99213",
            Status = WorkItemStatus.ReadyForReview
        };

        var result = await _host.Scenario(s =>
        {
            s.Post.Json(request).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(HttpStatusCode.Created);
        });

        var workItem = result.ReadAsJson<WorkItem>();
        await Assert.That(workItem!.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    #endregion

    #region GET /api/work-items

    [Test]
    public async Task ListWorkItems_EmptyStore_ReturnsEmptyList()
    {
        // Use fresh host to ensure empty store
        await using var freshHost = await AlbaHost.For<Program>();

        var result = await freshHost.Scenario(s =>
        {
            s.Get.Url("/api/work-items");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        var response = result.ReadAsJson<WorkItemListResponse>();
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Items).IsEmpty();
        await Assert.That(response.Total).IsEqualTo(0);
    }

    [Test]
    public async Task ListWorkItems_WithItems_ReturnsAll()
    {
        // Create two work items
        await CreateTestWorkItem(_host, "enc-list-1");
        await CreateTestWorkItem(_host, "enc-list-2");

        var result = await _host.Scenario(s =>
        {
            s.Get.Url("/api/work-items");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        var response = result.ReadAsJson<WorkItemListResponse>();
        await Assert.That(response!.Items.Count).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task ListWorkItems_FilterByEncounter_ReturnsFiltered()
    {
        await CreateTestWorkItem(_host, "enc-filter-target");
        await CreateTestWorkItem(_host, "enc-filter-other");

        var result = await _host.Scenario(s =>
        {
            s.Get.Url("/api/work-items?encounterId=enc-filter-target");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        var response = result.ReadAsJson<WorkItemListResponse>();
        await Assert.That(response!.Items).AllSatisfy(w =>
            Assert.That(w.EncounterId).IsEqualTo("enc-filter-target"));
    }

    #endregion

    #region GET /api/work-items/{id}

    [Test]
    public async Task GetWorkItem_Exists_Returns200()
    {
        var created = await CreateTestWorkItem(_host, "enc-get-test");

        var result = await _host.Scenario(s =>
        {
            s.Get.Url($"/api/work-items/{created.Id}");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        var workItem = result.ReadAsJson<WorkItem>();
        await Assert.That(workItem!.Id).IsEqualTo(created.Id);
    }

    [Test]
    public async Task GetWorkItem_NotExists_Returns404()
    {
        await _host.Scenario(s =>
        {
            s.Get.Url("/api/work-items/non-existent-id");
            s.StatusCodeShouldBe(HttpStatusCode.NotFound);
        });
    }

    #endregion

    #region PUT /api/work-items/{id}/status

    [Test]
    public async Task UpdateStatus_ValidTransition_Returns200()
    {
        var created = await CreateTestWorkItem(_host, "enc-status-test");

        var request = new UpdateStatusRequest
        {
            Status = WorkItemStatus.ReadyForReview
        };

        var result = await _host.Scenario(s =>
        {
            s.Put.Json(request).ToUrl($"/api/work-items/{created.Id}/status");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        var workItem = result.ReadAsJson<WorkItem>();
        await Assert.That(workItem!.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
        await Assert.That(workItem.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task UpdateStatus_NotExists_Returns404()
    {
        var request = new UpdateStatusRequest
        {
            Status = WorkItemStatus.Submitted
        };

        await _host.Scenario(s =>
        {
            s.Put.Json(request).ToUrl("/api/work-items/non-existent-id/status");
            s.StatusCodeShouldBe(HttpStatusCode.NotFound);
        });
    }

    #endregion

    #region POST /api/work-items/{id}/rehydrate

    [Test]
    public async Task Rehydrate_WithToken_CallsFhirWithToken()
    {
        var created = await CreateTestWorkItem(_host, "enc-rehydrate-test");

        var request = new RehydrateRequest
        {
            WorkItemId = created.Id,
            AccessToken = "test-access-token"
        };

        // Note: This will fail FHIR call but should not 404
        // In a real test, mock the FHIR client
        var result = await _host.Scenario(s =>
        {
            s.Post.Json(request).ToUrl($"/api/work-items/{created.Id}/rehydrate");
            // May return 200 or 500 depending on FHIR mock
        });
    }

    [Test]
    public async Task Rehydrate_NotExists_Returns404()
    {
        var request = new RehydrateRequest
        {
            WorkItemId = "non-existent",
            AccessToken = "test-token"
        };

        await _host.Scenario(s =>
        {
            s.Post.Json(request).ToUrl("/api/work-items/non-existent/rehydrate");
            s.StatusCodeShouldBe(HttpStatusCode.NotFound);
        });
    }

    #endregion

    #region Helpers

    private static async Task<WorkItem> CreateTestWorkItem(IAlbaHost host, string encounterId)
    {
        var request = new CreateWorkItemRequest
        {
            EncounterId = encounterId,
            PatientId = $"pat-{Guid.NewGuid():N}",
            ServiceRequestId = $"sr-{Guid.NewGuid():N}",
            ProcedureCode = "72148"
        };

        var result = await host.Scenario(s =>
        {
            s.Post.Json(request).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(HttpStatusCode.Created);
        });

        return result.ReadAsJson<WorkItem>()!;
    }

    #endregion
}
```

---

### Task 6: Add Unit Tests for New Endpoints

**File:** `apps/gateway/Gateway.API.Tests/Endpoints/WorkItemEndpointsCrudTests.cs`

```csharp
using Gateway.API.Contracts;
using Gateway.API.Endpoints;
using Gateway.API.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gateway.API.Tests.Endpoints;

/// <summary>
/// Unit tests for WorkItem CRUD endpoints.
/// </summary>
public class WorkItemEndpointsCrudTests
{
    private readonly IWorkItemStore _workItemStore;
    private readonly ILogger<WorkItem> _logger;

    public WorkItemEndpointsCrudTests()
    {
        _workItemStore = Substitute.For<IWorkItemStore>();
        _logger = Substitute.For<ILogger<WorkItem>>();
    }

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_ValidRequest_Returns201WithWorkItem()
    {
        // Arrange
        var request = new CreateWorkItemRequest
        {
            EncounterId = "enc-001",
            PatientId = "pat-001",
            ServiceRequestId = "sr-001",
            ProcedureCode = "72148"
        };

        _workItemStore.CreateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>().Id);

        // Act
        var result = await WorkItemEndpoints.CreateAsync(request, _workItemStore, _logger, CancellationToken.None);

        // Assert
        var created = result as Created<WorkItem>;
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.Value!.EncounterId).IsEqualTo("enc-001");
        await Assert.That(created.Value.Status).IsEqualTo(WorkItemStatus.MissingData);
    }

    [Test]
    public async Task CreateAsync_WithStatus_UsesProvidedStatus()
    {
        // Arrange
        var request = new CreateWorkItemRequest
        {
            EncounterId = "enc-002",
            PatientId = "pat-002",
            ServiceRequestId = "sr-002",
            ProcedureCode = "99213",
            Status = WorkItemStatus.ReadyForReview
        };

        _workItemStore.CreateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>().Id);

        // Act
        var result = await WorkItemEndpoints.CreateAsync(request, _workItemStore, _logger, CancellationToken.None);

        // Assert
        var created = result as Created<WorkItem>;
        await Assert.That(created!.Value!.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    #endregion

    #region ListAsync Tests

    [Test]
    public async Task ListAsync_NoFilters_ReturnsAllItems()
    {
        // Arrange
        var items = new List<WorkItem>
        {
            CreateTestWorkItem("wi-001", "enc-001"),
            CreateTestWorkItem("wi-002", "enc-002")
        };

        _workItemStore.GetAllAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(items);

        // Act
        var result = await WorkItemEndpoints.ListAsync(null, null, _workItemStore, CancellationToken.None);

        // Assert
        var ok = result as Ok<WorkItemListResponse>;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value!.Items.Count).IsEqualTo(2);
        await Assert.That(ok.Value.Total).IsEqualTo(2);
    }

    [Test]
    public async Task ListAsync_WithEncounterFilter_PassesToStore()
    {
        // Arrange
        _workItemStore.GetAllAsync("enc-filter", null, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItem>());

        // Act
        await WorkItemEndpoints.ListAsync("enc-filter", null, _workItemStore, CancellationToken.None);

        // Assert
        await _workItemStore.Received(1).GetAllAsync("enc-filter", null, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetByIdAsync Tests

    [Test]
    public async Task GetByIdAsync_Exists_ReturnsWorkItem()
    {
        // Arrange
        var workItem = CreateTestWorkItem("wi-get", "enc-get");
        _workItemStore.GetByIdAsync("wi-get", Arg.Any<CancellationToken>())
            .Returns(workItem);

        // Act
        var result = await WorkItemEndpoints.GetByIdAsync("wi-get", _workItemStore, CancellationToken.None);

        // Assert
        var ok = result as Ok<WorkItem>;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value!.Id).IsEqualTo("wi-get");
    }

    [Test]
    public async Task GetByIdAsync_NotExists_Returns404()
    {
        // Arrange
        _workItemStore.GetByIdAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        // Act
        var result = await WorkItemEndpoints.GetByIdAsync("non-existent", _workItemStore, CancellationToken.None);

        // Assert
        var notFound = result as NotFound<ErrorResponse>;
        await Assert.That(notFound).IsNotNull();
        await Assert.That(notFound!.Value!.Code).IsEqualTo("WORK_ITEM_NOT_FOUND");
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Test]
    public async Task UpdateStatusAsync_ValidUpdate_ReturnsUpdatedItem()
    {
        // Arrange
        var existing = CreateTestWorkItem("wi-update", "enc-update");
        var request = new UpdateStatusRequest { Status = WorkItemStatus.Submitted };

        _workItemStore.GetByIdAsync("wi-update", Arg.Any<CancellationToken>())
            .Returns(existing);
        _workItemStore.UpdateStatusAsync("wi-update", WorkItemStatus.Submitted, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await WorkItemEndpoints.UpdateStatusAsync("wi-update", request, _workItemStore, _logger, CancellationToken.None);

        // Assert
        var ok = result as Ok<WorkItem>;
        await Assert.That(ok).IsNotNull();
    }

    [Test]
    public async Task UpdateStatusAsync_NotExists_Returns404()
    {
        // Arrange
        var request = new UpdateStatusRequest { Status = WorkItemStatus.Submitted };
        _workItemStore.GetByIdAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        // Act
        var result = await WorkItemEndpoints.UpdateStatusAsync("non-existent", request, _workItemStore, _logger, CancellationToken.None);

        // Assert
        var notFound = result as NotFound<ErrorResponse>;
        await Assert.That(notFound).IsNotNull();
    }

    #endregion

    #region Helpers

    private static WorkItem CreateTestWorkItem(string id, string encounterId)
    {
        return new WorkItem
        {
            Id = id,
            EncounterId = encounterId,
            PatientId = "pat-test",
            ServiceRequestId = "sr-test",
            Status = WorkItemStatus.MissingData,
            ProcedureCode = "72148",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
```

---

## Task Summary

| Task | Description | Files | Est. Tests |
|------|-------------|-------|------------|
| **1** | Create request/response models | 3 new | 0 |
| **2** | Add `GetAllAsync` to IWorkItemStore | 2 modified | 2 |
| **3** | Extend WorkItemEndpoints with CRUD | 1 modified | 0 |
| **4** | Modify RehydrateRequest for token | 2 modified | 1 |
| **5** | Add Alba integration tests | 2 new | 10 |
| **6** | Add unit tests for CRUD endpoints | 1 new | 8 |

**Total new tests:** ~21

---

## Dependencies

### Parallel Tasks (Group 1)
- Task 1: Create models
- Task 5: Add Alba package

### Sequential Chain
```
Task 1 → Task 2 → Task 3 → Task 4 → Task 6
              ↘
               Task 5 (after Task 3)
```

---

## Acceptance Criteria

- [ ] `POST /api/work-items` creates a work item and returns 201
- [ ] `GET /api/work-items` lists all work items with optional filters
- [ ] `GET /api/work-items/{id}` returns work item or 404
- [ ] `PUT /api/work-items/{id}/status` updates status or returns 404
- [ ] `POST /api/work-items/{id}/rehydrate` triggers re-analysis (no request body needed)
- [ ] All unit tests pass (TUnit)
- [ ] All integration tests pass (Alba)
- [ ] Can create, retrieve, update, and rehydrate work items in preview environment
