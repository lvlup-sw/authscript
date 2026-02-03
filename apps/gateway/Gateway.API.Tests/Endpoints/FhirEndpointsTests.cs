// =============================================================================
// <copyright file="FhirEndpointsTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using System.Text.Json;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Gateway.API.Tests.Endpoints;

/// <summary>
/// Tests for <see cref="FhirEndpoints"/>.
/// </summary>
public class FhirEndpointsTests
{
    private readonly IFhirHttpClient _fhirClient = Substitute.For<IFhirHttpClient>();
    private readonly IOptions<AthenaOptions> _options;

    public FhirEndpointsTests()
    {
        _options = Options.Create(new AthenaOptions
        {
            FhirBaseUrl = "https://fhir.example.com",
            ClientId = "test-client",
            TokenEndpoint = "https://auth.example.com/token",
            PracticeId = "ah-practice-123"
        });
    }

    [Test]
    public async Task SearchPatientsAsync_WithName_ReturnsBundle()
    {
        // Arrange
        var bundle = JsonDocument.Parse("""{"resourceType": "Bundle", "entry": []}""").RootElement;
        _fhirClient.SearchAsync("Patient", "name=Test&ah-practice=ah-practice-123", Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundle));

        // Act
        var result = await FhirEndpoints.SearchPatientsAsync("Test", _fhirClient, _options);

        // Assert
        await Assert.That(result.Result).IsTypeOf<Ok<JsonElement>>();
        var okResult = (Ok<JsonElement>)result.Result;
        await Assert.That(okResult.Value.GetProperty("resourceType").GetString()).IsEqualTo("Bundle");
    }

    [Test]
    public async Task SearchPatientsAsync_WhenFhirFails_ReturnsProblem()
    {
        // Arrange
        var error = FhirError.Network("Connection refused");
        _fhirClient.SearchAsync("Patient", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(error));

        // Act
        var result = await FhirEndpoints.SearchPatientsAsync("Test", _fhirClient, _options);

        // Assert
        await Assert.That(result.Result).IsTypeOf<ProblemHttpResult>();
    }

    [Test]
    public async Task GetPatientAsync_WithValidId_ReturnsPatient()
    {
        // Arrange
        var patient = JsonDocument.Parse("""{"resourceType": "Patient", "id": "123"}""").RootElement;
        _fhirClient.ReadAsync("Patient", "123", Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(patient));

        // Act
        var result = await FhirEndpoints.GetPatientAsync("123", _fhirClient);

        // Assert
        await Assert.That(result.Result).IsTypeOf<Ok<JsonElement>>();
        var okResult = (Ok<JsonElement>)result.Result;
        await Assert.That(okResult.Value.GetProperty("id").GetString()).IsEqualTo("123");
    }

    [Test]
    public async Task GetPatientAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var error = FhirError.NotFound("Patient", "999");
        _fhirClient.ReadAsync("Patient", "999", Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(error));

        // Act
        var result = await FhirEndpoints.GetPatientAsync("999", _fhirClient);

        // Assert
        await Assert.That(result.Result).IsTypeOf<NotFound>();
    }

    [Test]
    public async Task SearchEncountersAsync_WithPatientId_ReturnsBundle()
    {
        // Arrange
        var bundle = JsonDocument.Parse("""{"resourceType": "Bundle", "entry": []}""").RootElement;
        _fhirClient.SearchAsync("Encounter", "patient=Patient/123&ah-practice=ah-practice-123", Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundle));

        // Act
        var result = await FhirEndpoints.SearchEncountersAsync("123", _fhirClient, _options);

        // Assert
        await Assert.That(result.Result).IsTypeOf<Ok<JsonElement>>();
        var okResult = (Ok<JsonElement>)result.Result;
        await Assert.That(okResult.Value.GetProperty("resourceType").GetString()).IsEqualTo("Bundle");
    }

    [Test]
    public async Task SearchEncountersAsync_WhenFhirFails_ReturnsProblem()
    {
        // Arrange
        var error = FhirError.Unauthorized("Token expired");
        _fhirClient.SearchAsync("Encounter", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(error));

        // Act
        var result = await FhirEndpoints.SearchEncountersAsync("123", _fhirClient, _options);

        // Assert
        await Assert.That(result.Result).IsTypeOf<ProblemHttpResult>();
    }

    [Test]
    public async Task SearchPatientsAsync_WithNoPracticeId_OmitsPracticeParam()
    {
        // Arrange
        var optionsNoPractice = Options.Create(new AthenaOptions
        {
            FhirBaseUrl = "https://fhir.example.com",
            ClientId = "test-client",
            TokenEndpoint = "https://auth.example.com/token",
            PracticeId = null
        });
        var bundle = JsonDocument.Parse("""{"resourceType": "Bundle", "entry": []}""").RootElement;
        _fhirClient.SearchAsync("Patient", "name=Test", Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundle));

        // Act
        var result = await FhirEndpoints.SearchPatientsAsync("Test", _fhirClient, optionsNoPractice);

        // Assert
        await Assert.That(result.Result).IsTypeOf<Ok<JsonElement>>();
    }
}
