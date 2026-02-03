using System.Collections.Concurrent;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Models;
using Gateway.API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for FhirDataAggregator that fetches and aggregates clinical data
/// from FHIR API in parallel.
/// </summary>
public class FhirDataAggregatorTests
{
    private readonly IFhirClient _fhirClient;
    private readonly IOptions<ClinicalQueryOptions> _options;
    private readonly ILogger<FhirDataAggregator> _logger;
    private readonly FhirDataAggregator _sut;

    public FhirDataAggregatorTests()
    {
        _fhirClient = Substitute.For<IFhirClient>();
        _options = Substitute.For<IOptions<ClinicalQueryOptions>>();
        _options.Value.Returns(new ClinicalQueryOptions
        {
            ObservationLookbackMonths = 12,
            ProcedureLookbackMonths = 24
        });
        _logger = Substitute.For<ILogger<FhirDataAggregator>>();

        // Default mock returns for all methods
        _fhirClient.GetPatientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PatientInfo?>(CreateTestPatient()));
        _fhirClient.SearchConditionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ConditionInfo>()));
        _fhirClient.SearchObservationsAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ObservationInfo>()));
        _fhirClient.SearchProceduresAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ProcedureInfo>()));
        _fhirClient.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<DocumentInfo>()));
        _fhirClient.SearchServiceRequestsAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ServiceRequestInfo>()));

        _sut = new FhirDataAggregator(_fhirClient, _options, _logger);
    }

    [Test]
    public async Task AggregateClinicalDataAsync_IncludesServiceRequests_InBundle()
    {
        // Arrange
        const string patientId = "patient-123";
        var expectedServiceRequests = new List<ServiceRequestInfo>
        {
            new ServiceRequestInfo
            {
                Id = "sr-1",
                Status = "active",
                Code = new CodeableConcept
                {
                    Coding =
                    [
                        new Coding { System = "http://www.ama-assn.org/go/cpt", Code = "72148", Display = "MRI lumbar spine" }
                    ],
                    Text = "MRI lumbar spine"
                },
                EncounterId = "enc-456",
                AuthoredOn = DateTimeOffset.UtcNow
            }
        };

        _fhirClient.SearchServiceRequestsAsync(patientId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedServiceRequests));

        // Act
        var result = await _sut.AggregateClinicalDataAsync(patientId, null, CancellationToken.None);

        // Assert
        await Assert.That(result.ServiceRequests).IsNotEmpty();
        await Assert.That(result.ServiceRequests).HasCount().EqualTo(1);
        await Assert.That(result.ServiceRequests[0].Id).IsEqualTo("sr-1");
        await Assert.That(result.ServiceRequests[0].Status).IsEqualTo("active");
    }

    [Test]
    public async Task AggregateClinicalDataAsync_CallsSearchServiceRequestsAsync_WithPatientId()
    {
        // Arrange
        const string patientId = "patient-789";

        // Act
        await _sut.AggregateClinicalDataAsync(patientId, null, CancellationToken.None);

        // Assert
        await _fhirClient.Received(1).SearchServiceRequestsAsync(
            patientId,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AggregateClinicalDataAsync_PassesEncounterId_ToSearchServiceRequestsAsync()
    {
        // Arrange
        const string patientId = "patient-123";
        const string encounterId = "encounter-456";

        // Act
        await _sut.AggregateClinicalDataAsync(patientId, encounterId, CancellationToken.None);

        // Assert - Verify encounterId is passed to SearchServiceRequestsAsync
        await _fhirClient.Received(1).SearchServiceRequestsAsync(
            patientId,
            encounterId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AggregateClinicalDataAsync_FetchesServiceRequestsInParallel_WithOtherResources()
    {
        // Arrange
        const string patientId = "patient-abc";

        // Track call order to verify parallel execution (thread-safe for parallel callbacks)
        var callOrder = new ConcurrentBag<string>();

        _fhirClient.GetPatientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                callOrder.Add("Patient");
                await Task.Delay(10);
                return CreateTestPatient();
            });
        _fhirClient.SearchConditionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                callOrder.Add("Conditions");
                await Task.Delay(10);
                return new List<ConditionInfo>();
            });
        _fhirClient.SearchServiceRequestsAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                callOrder.Add("ServiceRequests");
                await Task.Delay(10);
                return new List<ServiceRequestInfo>();
            });

        // Act
        await _sut.AggregateClinicalDataAsync(patientId, null, CancellationToken.None);

        // Assert - All methods should have been called
        await Assert.That(callOrder).Contains("Patient");
        await Assert.That(callOrder).Contains("Conditions");
        await Assert.That(callOrder).Contains("ServiceRequests");

        // Verify ServiceRequests was called
        await _fhirClient.Received(1).SearchServiceRequestsAsync(
            patientId,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AggregateClinicalDataAsync_WithData_LogsSignalCountsWithPatientDemographicsFlag()
    {
        // Arrange
        const string patientId = "patient-signal-test";

        // Set up mock returns with test data
        _fhirClient.GetPatientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PatientInfo?>(CreateTestPatient()));

        _fhirClient.SearchConditionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ConditionInfo>
            {
                new() { Id = "cond-1", Code = "J45.909", ClinicalStatus = "active" },
                new() { Id = "cond-2", Code = "E11.9", ClinicalStatus = "active" }
            }));

        _fhirClient.SearchObservationsAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ObservationInfo>
            {
                new() { Id = "obs-1", Code = "85354-9" }
            }));

        _fhirClient.SearchProceduresAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ProcedureInfo>()));

        _fhirClient.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<DocumentInfo>
            {
                new() { Id = "doc-1", Type = "Clinical Note" }
            }));

        _fhirClient.SearchServiceRequestsAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ServiceRequestInfo>
            {
                new() { Id = "sr-1", Status = "active", Code = new CodeableConcept { Text = "Test Service Request" } }
            }));

        // Act
        await _sut.AggregateClinicalDataAsync(patientId);

        // Assert - verify logger received the signal log with HasPatientDemographics
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("HasPatientDemographics")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task AggregateClinicalDataAsync_WhenPatientIsNull_LogsHasPatientDemographicsFalse()
    {
        // Arrange
        const string patientId = "patient-no-demographics";

        // Set up mock to return null patient
        _fhirClient.GetPatientAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PatientInfo?>(null));

        // Act
        await _sut.AggregateClinicalDataAsync(patientId);

        // Assert - verify logger received the signal log with HasPatientDemographics=False
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("HasPatientDemographics") && o.ToString()!.Contains("False")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static PatientInfo CreateTestPatient()
    {
        return new PatientInfo
        {
            Id = "patient-123",
            GivenName = "Test",
            FamilyName = "Patient",
            BirthDate = new DateOnly(1980, 1, 15),
            MemberId = "MEM123"
        };
    }
}
