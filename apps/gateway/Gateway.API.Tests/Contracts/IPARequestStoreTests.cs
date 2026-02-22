namespace Gateway.API.Tests.Contracts;

using Gateway.API.Contracts;
using Gateway.API.GraphQL.Models;
using NSubstitute;

public sealed class IPARequestStoreTests
{
    [Test]
    public async Task IPARequestStore_InterfaceCanBeMocked()
    {
        // Arrange
        var store = Substitute.For<IPARequestStore>();

        // Act & Assert - Verify key method signatures compile
        await store.GetAllAsync();
        await store.GetByIdAsync("PA-001");
        await store.CreateAsync(CreateTestModel(), "a-195900.E-60178");
        await store.UpdateFieldsAsync("PA-001", "Diagnosis", "D12.3", "2026-01-15", "Office", "Summary", null);
        await store.ApplyAnalysisResultAsync("PA-001", "AI summary", 85, new List<CriterionModel>(), "M54.5", "Low Back Pain");
        await store.SubmitAsync("PA-001", 120);
        await store.AddReviewTimeAsync("PA-001", 30);
        await store.DeleteAsync("PA-001");
        await store.GetStatsAsync();
        await store.GetActivityAsync();
    }

    [Test]
    public async Task IPARequestStore_HasRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(IPARequestStore);

        // Act & Assert - Verify interface exists and has required methods
        await Assert.That(interfaceType).IsNotNull();
        await Assert.That(interfaceType.IsInterface).IsTrue();

        await Assert.That(interfaceType.GetMethod("GetAllAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("GetByIdAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("CreateAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("UpdateFieldsAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("ApplyAnalysisResultAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("SubmitAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("AddReviewTimeAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("DeleteAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("GetStatsAsync")).IsNotNull();
        await Assert.That(interfaceType.GetMethod("GetActivityAsync")).IsNotNull();
    }

    [Test]
    public async Task IPARequestStore_GetAllAsync_ReturnsCorrectType()
    {
        // Arrange
        var method = typeof(IPARequestStore).GetMethod("GetAllAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<IReadOnlyList<PARequestModel>>));
    }

    [Test]
    public async Task IPARequestStore_GetByIdAsync_ReturnsNullablePARequest()
    {
        // Arrange
        var method = typeof(IPARequestStore).GetMethod("GetByIdAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<PARequestModel?>));
    }

    [Test]
    public async Task IPARequestStore_DeleteAsync_ReturnsBool()
    {
        // Arrange
        var method = typeof(IPARequestStore).GetMethod("DeleteAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<bool>));
    }

    [Test]
    public async Task IPARequestStore_GetStatsAsync_ReturnsStatsModel()
    {
        // Arrange
        var method = typeof(IPARequestStore).GetMethod("GetStatsAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<PAStatsModel>));
    }

    [Test]
    public async Task IPARequestStore_GetActivityAsync_ReturnsActivityList()
    {
        // Arrange
        var method = typeof(IPARequestStore).GetMethod("GetActivityAsync");

        // Assert
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<IReadOnlyList<ActivityItemModel>>));
    }

    private static PARequestModel CreateTestModel() => new()
    {
        Id = "PA-001",
        PatientId = "patient-1",
        Patient = new PatientModel
        {
            Id = "patient-1",
            Name = "John Doe",
            Mrn = "MRN-001",
            Dob = "1990-01-01",
            MemberId = "M-12345",
            Payer = "Blue Cross",
            Phone = "555-0100",
            Address = "123 Main St"
        },
        ProcedureCode = "27447",
        ProcedureName = "Total Knee Replacement",
        Diagnosis = "Primary osteoarthritis",
        DiagnosisCode = "M17.11",
        Payer = "Blue Cross",
        Provider = "Dr. Smith",
        ProviderNpi = "1234567890",
        ServiceDate = "2026-03-15",
        PlaceOfService = "Outpatient Hospital",
        ClinicalSummary = "",
        Status = "draft",
        Confidence = 0,
        CreatedAt = "2026-02-21T10:00:00Z",
        UpdatedAt = "2026-02-21T10:00:00Z",
        Criteria = new List<CriterionModel>()
    };
}
