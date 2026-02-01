using Alba;
using Gateway.API.Models;

namespace Gateway.API.Tests.Integration;

/// <summary>
/// Alba-based integration tests for WorkItem CRUD endpoints.
/// </summary>
[Category("Integration")]
[ClassDataSource<GatewayAlbaBootstrap>(Shared = SharedType.PerTestSession)]
public class WorkItemEndpointsIntegrationTests
{
    private readonly GatewayAlbaBootstrap _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkItemEndpointsIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The Alba bootstrap fixture.</param>
    public WorkItemEndpointsIntegrationTests(GatewayAlbaBootstrap fixture)
    {
        _fixture = fixture;
    }

    #region POST /api/work-items (Create)

    [Test]
    public async Task CreateWorkItem_ValidRequest_Returns201()
    {
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new CreateWorkItemRequest
            {
                EncounterId = "enc-integration-1",
                PatientId = "pat-integration-1",
                ServiceRequestId = "sr-integration-1",
                ProcedureCode = "72148"
            }).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(201);
        }).ConfigureAwait(false);

        var workItem = result.ReadAsJson<WorkItem>();
        await Assert.That(workItem).IsNotNull();
        await Assert.That(workItem!.Status).IsEqualTo(WorkItemStatus.MissingData);
        await Assert.That(workItem.EncounterId).IsEqualTo("enc-integration-1");
        await Assert.That(workItem.PatientId).IsEqualTo("pat-integration-1");
    }

    [Test]
    public async Task CreateWorkItem_WithCustomStatus_UsesProvidedStatus()
    {
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new CreateWorkItemRequest
            {
                EncounterId = "enc-custom-status",
                PatientId = "pat-custom-status",
                ServiceRequestId = "sr-custom-status",
                ProcedureCode = "99213",
                Status = WorkItemStatus.ReadyForReview
            }).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(201);
        }).ConfigureAwait(false);

        var workItem = result.ReadAsJson<WorkItem>();
        await Assert.That(workItem!.Status).IsEqualTo(WorkItemStatus.ReadyForReview);
    }

    #endregion

    #region GET /api/work-items (List)

    [Test]
    public async Task ListWorkItems_ReturnsListResponse()
    {
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/work-items");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var response = result.ReadAsJson<WorkItemListResponse>();
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Items).IsNotNull();
    }

    [Test]
    public async Task ListWorkItems_WithStatusFilter_ReturnsFilteredResults()
    {
        // First create a work item with specific status
        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new CreateWorkItemRequest
            {
                EncounterId = "enc-filter-test",
                PatientId = "pat-filter-test",
                ServiceRequestId = "sr-filter-test",
                ProcedureCode = "72148",
                Status = WorkItemStatus.Submitted
            }).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(201);
        }).ConfigureAwait(false);

        // List with status filter
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/work-items?status=Submitted");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var response = result.ReadAsJson<WorkItemListResponse>();
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Items.All(i => i.Status == WorkItemStatus.Submitted)).IsTrue();
        await Assert.That(response.Items.Any(i => i.EncounterId == "enc-filter-test")).IsTrue();
    }

    #endregion

    #region GET /api/work-items/{id} (GetById)

    [Test]
    public async Task GetWorkItemById_Exists_ReturnsWorkItem()
    {
        // First create a work item
        var createResult = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new CreateWorkItemRequest
            {
                EncounterId = "enc-get-by-id",
                PatientId = "pat-get-by-id",
                ServiceRequestId = "sr-get-by-id",
                ProcedureCode = "72148"
            }).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(201);
        }).ConfigureAwait(false);

        var created = createResult.ReadAsJson<WorkItem>();

        // Get by ID
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url($"/api/work-items/{created!.Id}");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var workItem = result.ReadAsJson<WorkItem>();
        await Assert.That(workItem!.Id).IsEqualTo(created!.Id);
        await Assert.That(workItem.EncounterId).IsEqualTo("enc-get-by-id");
    }

    [Test]
    public async Task GetWorkItemById_NotExists_Returns404()
    {
        await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/work-items/nonexistent-id");
            s.StatusCodeShouldBe(404);
        }).ConfigureAwait(false);
    }

    #endregion

    #region PUT /api/work-items/{id}/status (UpdateStatus)

    [Test]
    public async Task UpdateStatus_Exists_ReturnsUpdatedWorkItem()
    {
        // First create a work item
        var createResult = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new CreateWorkItemRequest
            {
                EncounterId = "enc-update-status",
                PatientId = "pat-update-status",
                ServiceRequestId = "sr-update-status",
                ProcedureCode = "72148"
            }).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(201);
        }).ConfigureAwait(false);

        var created = createResult.ReadAsJson<WorkItem>();

        // Update status
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Put.Json(new UpdateStatusRequest { Status = WorkItemStatus.Submitted })
                .ToUrl($"/api/work-items/{created!.Id}/status");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var updated = result.ReadAsJson<WorkItem>();
        await Assert.That(updated!.Status).IsEqualTo(WorkItemStatus.Submitted);
    }

    [Test]
    public async Task UpdateStatus_NotExists_Returns404()
    {
        await _fixture.Host.Scenario(s =>
        {
            s.Put.Json(new UpdateStatusRequest { Status = WorkItemStatus.Submitted })
                .ToUrl("/api/work-items/nonexistent-id/status");
            s.StatusCodeShouldBe(404);
        }).ConfigureAwait(false);
    }

    #endregion

    #region POST /api/work-items/{id}/rehydrate (Rehydrate)

    [Test]
    public async Task Rehydrate_NotExists_Returns404()
    {
        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new RehydrateRequest())
                .ToUrl("/api/work-items/nonexistent-id/rehydrate");
            s.StatusCodeShouldBe(404);
        }).ConfigureAwait(false);
    }

    [Test]
    public async Task Rehydrate_Exists_ReturnsRehydrateResponse()
    {
        // First create a work item
        var createResult = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new CreateWorkItemRequest
            {
                EncounterId = "enc-rehydrate",
                PatientId = "pat-rehydrate",
                ServiceRequestId = "sr-rehydrate",
                ProcedureCode = "72148"
            }).ToUrl("/api/work-items");
            s.StatusCodeShouldBe(201);
        }).ConfigureAwait(false);

        var created = createResult.ReadAsJson<WorkItem>();

        // Rehydrate with access token
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(new RehydrateRequest { AccessToken = "test-access-token" })
                .ToUrl($"/api/work-items/{created!.Id}/rehydrate");
            s.StatusCodeShouldBeOk();
        }).ConfigureAwait(false);

        var response = result.ReadAsJson<RehydrateResponse>();
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.WorkItemId).IsEqualTo(created!.Id);
        await Assert.That(response.Message).IsNotNull();
    }

    #endregion
}
