// =============================================================================
// <copyright file="MockDataServiceTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Gateway.API.GraphQL.Models;
using Gateway.API.GraphQL.Queries;
using Gateway.API.Services;
using NSubstitute;

namespace Gateway.API.Tests.Services;

public class MockDataServiceTests
{
    [Test]
    public async Task MockDataService_ImplementsIDataService()
    {
        // Arrange & Act
        var implements = typeof(MockDataService).GetInterfaces()
            .Any(i => i.Name == "IDataService");

        // Assert
        await Assert.That(implements).IsTrue();
    }

    [Test]
    public async Task IDataService_CanBeAssignedFromMockDataService()
    {
        // Arrange
        var mock = new MockDataService();

        // Act
        IDataService service = mock;
        var requests = service.GetPARequests();

        // Assert
        await Assert.That(requests).IsNotNull();
        await Assert.That(requests.Count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task ApprovePA_WaitingForInsurance_TransitionsToApproved()
    {
        // Arrange
        var service = new MockDataService();

        // Act — PA-004 is bootstrapped with status "waiting_for_insurance"
        var result = service.ApprovePA("PA-004");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Status).IsEqualTo("approved");
    }

    [Test]
    public async Task ApprovePA_DraftStatus_ReturnsNull()
    {
        // Arrange
        var service = new MockDataService();
        var created = service.CreatePARequest(
            new PatientModel
            {
                Id = "999",
                Name = "Test Patient",
                Mrn = "MRN-999",
                Dob = "1990-01-01",
                MemberId = "INS-999",
                Payer = "Test Payer",
                Address = "123 Main St",
                Phone = "555-0000",
            },
            "72148",
            "M54.5",
            "Low back pain");

        // Act
        var result = service.ApprovePA(created.Id);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task DenyPA_WaitingForInsurance_TransitionsToDenied()
    {
        // Arrange
        var service = new MockDataService();

        // Act
        var result = service.DenyPA("PA-004", "Insufficient documentation");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Status).IsEqualTo("denied");
    }

    [Test]
    public async Task DenyPA_WaitingForInsurance_StoresDenialReason()
    {
        // Arrange
        var service = new MockDataService();

        // Act — PA-004 is bootstrapped with status "waiting_for_insurance"
        var result = service.DenyPA("PA-004", "Insufficient documentation");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Status).IsEqualTo("denied");
        await Assert.That(result!.DenialReason).IsEqualTo("Insufficient documentation");
    }

    [Test]
    public async Task DenyPA_WrongStatus_ReturnsNull()
    {
        // Arrange
        var service = new MockDataService();

        // Act — PA-001 is "ready", not "waiting_for_insurance"
        var result = service.DenyPA("PA-001", "Test reason");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Query_GetPARequests_WorksWithIDataService()
    {
        // Arrange
        var mockService = Substitute.For<IDataService>();
        mockService.GetPARequests().Returns(new List<PARequestModel>
        {
            new()
            {
                Id = "PA-TEST",
                Status = "draft",
                PatientId = "T001",
                Patient = new PatientModel { Id = "T001", Name = "Test Patient", Mrn = "T001", Dob = "2000-01-01", MemberId = "M001", Payer = "Test", Address = "", Phone = "" },
                ProcedureCode = "72148",
                ProcedureName = "MRI Lumbar Spine w/o Contrast",
                Diagnosis = "Low Back Pain",
                DiagnosisCode = "M54.5",
                Payer = "Test",
                Provider = "Dr. Test",
                ProviderNpi = "0000000000",
                ServiceDate = "January 1, 2025",
                PlaceOfService = "Outpatient",
                ClinicalSummary = "Test summary",
                Confidence = 0,
                CreatedAt = "",
                UpdatedAt = "",
                Criteria = [],
            },
        });

        // Act
        var query = new Query();
        var result = query.GetPARequests(mockService);

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo("PA-TEST");
    }
}
