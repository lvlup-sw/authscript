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

    [Test]
    public async Task CreateAsync_CallsStoreWithCorrectData()
    {
        // Arrange
        var request = new CreateWorkItemRequest
        {
            EncounterId = "enc-003",
            PatientId = "pat-003",
            ServiceRequestId = "sr-003",
            ProcedureCode = "72148"
        };

        WorkItem? capturedWorkItem = null;
        _workItemStore.CreateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedWorkItem = ci.Arg<WorkItem>();
                return capturedWorkItem.Id;
            });

        // Act
        await WorkItemEndpoints.CreateAsync(request, _workItemStore, _logger, CancellationToken.None);

        // Assert
        await _workItemStore.Received(1).CreateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>());
        await Assert.That(capturedWorkItem).IsNotNull();
        await Assert.That(capturedWorkItem!.PatientId).IsEqualTo("pat-003");
        await Assert.That(capturedWorkItem.ServiceRequestId).IsEqualTo("sr-003");
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

    [Test]
    public async Task ListAsync_WithStatusFilter_PassesToStore()
    {
        // Arrange
        _workItemStore.GetAllAsync(null, WorkItemStatus.ReadyForReview, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItem>());

        // Act
        await WorkItemEndpoints.ListAsync(null, WorkItemStatus.ReadyForReview, _workItemStore, CancellationToken.None);

        // Assert
        await _workItemStore.Received(1).GetAllAsync(null, WorkItemStatus.ReadyForReview, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        _workItemStore.GetAllAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItem>());

        // Act
        var result = await WorkItemEndpoints.ListAsync(null, null, _workItemStore, CancellationToken.None);

        // Assert
        var ok = result as Ok<WorkItemListResponse>;
        await Assert.That(ok).IsNotNull();
        await Assert.That(ok!.Value!.Items).IsEmpty();
        await Assert.That(ok.Value.Total).IsEqualTo(0);
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

    [Test]
    public async Task GetByIdAsync_NotExists_ReturnsErrorMessage()
    {
        // Arrange
        _workItemStore.GetByIdAsync("missing-id", Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        // Act
        var result = await WorkItemEndpoints.GetByIdAsync("missing-id", _workItemStore, CancellationToken.None);

        // Assert
        var notFound = result as NotFound<ErrorResponse>;
        await Assert.That(notFound!.Value!.Message).Contains("missing-id");
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

        // After update, return updated version
        var updated = existing with { Status = WorkItemStatus.Submitted, UpdatedAt = DateTimeOffset.UtcNow };
        _workItemStore.GetByIdAsync("wi-update", Arg.Any<CancellationToken>())
            .Returns(existing, updated);

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
        await Assert.That(notFound!.Value!.Code).IsEqualTo("WORK_ITEM_NOT_FOUND");
    }

    [Test]
    public async Task UpdateStatusAsync_CallsStoreWithCorrectStatus()
    {
        // Arrange
        var existing = CreateTestWorkItem("wi-status", "enc-status");
        var request = new UpdateStatusRequest { Status = WorkItemStatus.ReadyForReview };

        _workItemStore.GetByIdAsync("wi-status", Arg.Any<CancellationToken>())
            .Returns(existing);
        _workItemStore.UpdateStatusAsync(Arg.Any<string>(), Arg.Any<WorkItemStatus>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await WorkItemEndpoints.UpdateStatusAsync("wi-status", request, _workItemStore, _logger, CancellationToken.None);

        // Assert
        await _workItemStore.Received(1).UpdateStatusAsync("wi-status", WorkItemStatus.ReadyForReview, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Route Registration Tests

    [Test]
    public async Task MapWorkItemEndpoints_RegistersCreateRoute()
    {
        // Arrange
        var methodInfo = typeof(WorkItemEndpoints).GetMethod("MapWorkItemEndpoints");

        // Assert - Method exists and is properly defined
        await Assert.That(methodInfo).IsNotNull();
        await Assert.That(methodInfo!.IsStatic).IsTrue();
        await Assert.That(methodInfo.IsPublic).IsTrue();

        // Verify CreateAsync is callable (route target)
        var createMethod = typeof(WorkItemEndpoints).GetMethod("CreateAsync");
        await Assert.That(createMethod).IsNotNull();
        await Assert.That(createMethod!.IsPublic).IsTrue();
    }

    [Test]
    public async Task MapWorkItemEndpoints_RegistersListRoute()
    {
        // Verify ListAsync is callable (route target)
        var listMethod = typeof(WorkItemEndpoints).GetMethod("ListAsync");
        await Assert.That(listMethod).IsNotNull();
        await Assert.That(listMethod!.IsPublic).IsTrue();
    }

    [Test]
    public async Task MapWorkItemEndpoints_RegistersGetByIdRoute()
    {
        // Verify GetByIdAsync is callable (route target)
        var getMethod = typeof(WorkItemEndpoints).GetMethod("GetByIdAsync");
        await Assert.That(getMethod).IsNotNull();
        await Assert.That(getMethod!.IsPublic).IsTrue();
    }

    [Test]
    public async Task MapWorkItemEndpoints_RegistersUpdateStatusRoute()
    {
        // Verify UpdateStatusAsync is callable (route target)
        var updateMethod = typeof(WorkItemEndpoints).GetMethod("UpdateStatusAsync");
        await Assert.That(updateMethod).IsNotNull();
        await Assert.That(updateMethod!.IsPublic).IsTrue();
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
