// =============================================================================
// <copyright file="QueryTests.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Tests.GraphQL;

using Gateway.API.Contracts;
using Gateway.API.GraphQL.Models;
using Gateway.API.GraphQL.Queries;
using Gateway.API.Services;
using NSubstitute;

/// <summary>
/// Tests for GraphQL Query resolvers verifying delegation to services.
/// </summary>
public sealed class QueryTests
{
    private readonly ReferenceDataService _refData = new();
    private readonly IPARequestStore _store = Substitute.For<IPARequestStore>();
    private readonly Query _query = new();

    #region Reference Data Tests

    [Test]
    public async Task GetProcedures_ReturnsProceduresFromReferenceData()
    {
        var result = _query.GetProcedures(_refData);

        await Assert.That(result.Count).IsEqualTo(10);
        await Assert.That(result[0].Code).IsEqualTo("72148");
    }

    [Test]
    public async Task GetMedications_ReturnsMedicationsFromReferenceData()
    {
        var result = _query.GetMedications(_refData);

        await Assert.That(result.Count).IsEqualTo(6);
        await Assert.That(result[0].Code).IsEqualTo("J1745");
    }

    [Test]
    public async Task GetProviders_ReturnsProvidersFromReferenceData()
    {
        var result = _query.GetProviders(_refData);

        await Assert.That(result.Count).IsEqualTo(4);
        await Assert.That(result[0].Id).IsEqualTo("DR001");
    }

    [Test]
    public async Task GetPayers_ReturnsPayersFromReferenceData()
    {
        var result = _query.GetPayers(_refData);

        await Assert.That(result.Count).IsEqualTo(6);
        await Assert.That(result[0].Id).IsEqualTo("BCBS");
    }

    [Test]
    public async Task GetDiagnoses_ReturnsDiagnosesFromReferenceData()
    {
        var result = _query.GetDiagnoses(_refData);

        await Assert.That(result.Count).IsEqualTo(14);
        await Assert.That(result[0].Code).IsEqualTo("M54.5");
    }

    #endregion

    #region PA Request Data Tests

    [Test]
    public async Task GetPARequests_DelegatesToStore()
    {
        var expected = new List<PARequestModel>();
        _store.GetAllAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _query.GetPARequests(_store, CancellationToken.None);

        await Assert.That(result).IsEqualTo(expected);
        await _store.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetPARequest_DelegatesToStore()
    {
        PARequestModel? expected = null;
        _store.GetByIdAsync("PA-001", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _query.GetPARequest("PA-001", _store, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _store.Received(1).GetByIdAsync("PA-001", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetPAStats_DelegatesToStore()
    {
        var expected = new PAStatsModel { Total = 5, Ready = 2, Submitted = 1, WaitingForInsurance = 1, Attention = 1 };
        _store.GetStatsAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _query.GetPAStats(_store, CancellationToken.None);

        await Assert.That(result.Total).IsEqualTo(5);
        await _store.Received(1).GetStatsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetActivity_DelegatesToStore()
    {
        var expected = new List<ActivityItemModel>();
        _store.GetActivityAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _query.GetActivity(_store, CancellationToken.None);

        await Assert.That(result).IsEqualTo(expected);
        await _store.Received(1).GetActivityAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
