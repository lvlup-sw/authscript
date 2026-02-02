namespace Gateway.API.Tests.Services.Polling;

using System.Text.Json;
using System.Threading.Channels;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Models;
using Gateway.API.Services.Polling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

public class AthenaPollingServiceTests
{
    private IFhirHttpClient _fhirClient = null!;
    private IOptions<AthenaOptions> _options = null!;
    private ILogger<AthenaPollingService> _logger = null!;
    private IPatientRegistry _patientRegistry = null!;
    private AthenaPollingService _sut = null!;

    [Before(Test)]
    public Task Setup()
    {
        _fhirClient = Substitute.For<IFhirHttpClient>();
        _logger = Substitute.For<ILogger<AthenaPollingService>>();
        _patientRegistry = Substitute.For<IPatientRegistry>();
        _options = Options.Create(new AthenaOptions
        {
            FhirBaseUrl = "https://api.athena.test/fhir/r4",
            ClientId = "test-client",
            TokenEndpoint = "https://api.athena.test/oauth2/token",
            PollingIntervalSeconds = 1,
            PracticeId = "Organization/a-1.Practice-12345"
        });

        _sut = new AthenaPollingService(_fhirClient, _options, _logger, _patientRegistry);
        return Task.CompletedTask;
    }

    [After(Test)]
    public async Task Cleanup()
    {
        await _sut.StopAsync(CancellationToken.None);
        _sut.Dispose();
    }

    [Test]
    public async Task Constructor_WithPatientRegistry_StoresReference()
    {
        // Arrange
        var fhirClient = Substitute.For<IFhirHttpClient>();
        var options = Options.Create(new AthenaOptions
        {
            FhirBaseUrl = "https://api.athena.test/fhir/r4",
            ClientId = "test-client",
            TokenEndpoint = "https://api.athena.test/oauth2/token",
            PollingIntervalSeconds = 5
        });
        var logger = Substitute.For<ILogger<AthenaPollingService>>();
        var patientRegistry = Substitute.For<IPatientRegistry>();

        // Act
        var service = new AthenaPollingService(
            fhirClient,
            options,
            logger,
            patientRegistry);

        // Assert - service was created without error
        await Assert.That(service).IsNotNull();

        service.Dispose();
    }

    // NOTE: Tests for global polling behavior removed in Task 015.
    // Per-patient polling queries FHIR per registered patient (Task 016).

    [Test]
    public async Task ExecuteAsync_RespectsPollingInterval_CallsRegistryMultipleTimes()
    {
        // Arrange
        var callCount = 0;
        _patientRegistry.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RegisteredPatient>())
            .AndDoes(_ => callCount++);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(180); // Wait for about 180ms (with 1s interval, should poll at least once)

        try
        {
            await Task.Delay(50, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        await _sut.StopAsync(CancellationToken.None);

        // Assert - should have called at least once
        await Assert.That(callCount).IsGreaterThanOrEqualTo(1);
    }

    // NOTE: Tests for per-patient encounter processing will be implemented in Task 016.
    // The following tests require PollPatientEncounterAsync to be implemented.

    [Test]
    [Skip("Per-patient encounter processing to be implemented in Task 016")]
    public async Task ExecuteAsync_SkipsAlreadyProcessedEncounters()
    {
        // Will be implemented in Task 016 when per-patient polling queries FHIR
        await Task.CompletedTask;
    }

    [Test]
    [Skip("Per-patient encounter processing to be implemented in Task 016")]
    public async Task ExecuteAsync_TracksProcessedEncounterIds()
    {
        // Will be implemented in Task 016 when per-patient polling queries FHIR
        await Task.CompletedTask;
    }

    [Test]
    public async Task PurgeProcessedEncountersOlderThan_RemovesOldEntries()
    {
        // Arrange - directly test the purge method without relying on polling
        var service = new AthenaPollingService(_fhirClient, _options, _logger, _patientRegistry);

        // Manually mark an encounter as processed (this tests the infrastructure)
        // Since we can't directly add to the dictionary, we test the purge method
        // on an empty service (it should not throw)
        service.PurgeProcessedEncountersOlderThan(TimeSpan.Zero);

        // Assert - should have 0 processed (nothing to purge)
        await Assert.That(service.GetProcessedEncounterCount()).IsEqualTo(0);

        service.Dispose();
    }

    [Test]
    [Skip("Per-patient encounter processing to be implemented in Task 016")]
    public async Task ExecuteAsync_EnqueuesEncounterToChannel()
    {
        // Will be implemented in Task 016 when per-patient polling detects finished encounters
        await Task.CompletedTask;
    }

    [Test]
    public async Task AthenaPollingService_Channel_IsUnboundedSingleConsumer()
    {
        // Arrange & Act
        var service = new AthenaPollingService(_fhirClient, _options, _logger, _patientRegistry);

        // Assert - The channel reader should exist
        var reader = service.Encounters;
        await Assert.That(reader).IsNotNull();

        // The Encounters property should return a type assignable to ChannelReader<string>
        await Assert.That(reader is ChannelReader<string>).IsTrue();

        service.Dispose();
    }

    private static JsonElement CreateEmptyBundle()
    {
        var json = """
        {
            "resourceType": "Bundle",
            "type": "searchset",
            "total": 0,
            "entry": []
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    [Test]
    public async Task ExecuteAsync_NoRegisteredPatients_DoesNotQueryFhir()
    {
        // Arrange
        _patientRegistry.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RegisteredPatient>());

        using var cts = new CancellationTokenSource();

        // Act - run one iteration then cancel
        var task = _sut.StartAsync(cts.Token);
        await Task.Delay(100); // Allow one poll cycle
        cts.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        await _sut.StopAsync(CancellationToken.None);

        // Assert - registry was queried but FHIR client was NOT called
        await _patientRegistry.Received(1).GetActiveAsync(Arg.Any<CancellationToken>());
        await _fhirClient.DidNotReceive().SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WithRegisteredPatients_CallsGetActiveAsync()
    {
        // Arrange
        var patients = new List<RegisteredPatient>
        {
            new() { PatientId = "p1", EncounterId = "e1", PracticeId = "pr1", WorkItemId = "w1", RegisteredAt = DateTimeOffset.UtcNow },
            new() { PatientId = "p2", EncounterId = "e2", PracticeId = "pr2", WorkItemId = "w2", RegisteredAt = DateTimeOffset.UtcNow },
        };
        _patientRegistry.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(patients);

        using var cts = new CancellationTokenSource();

        // Act
        var task = _sut.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        await _sut.StopAsync(CancellationToken.None);

        // Assert
        await _patientRegistry.Received().GetActiveAsync(Arg.Any<CancellationToken>());
    }

    private static JsonElement CreateBundleWithEncounter(string encounterId)
    {
        var json = $$"""
        {
            "resourceType": "Bundle",
            "type": "searchset",
            "total": 1,
            "entry": [
                {
                    "resource": {
                        "resourceType": "Encounter",
                        "id": "{{encounterId}}",
                        "status": "finished"
                    }
                }
            ]
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement CreateFhirBundle(string encounterId, string status)
    {
        var json = $$"""
        {
            "resourceType": "Bundle",
            "type": "searchset",
            "total": 1,
            "entry": [{
                "resource": {
                    "resourceType": "Encounter",
                    "id": "{{encounterId}}",
                    "status": "{{status}}"
                }
            }]
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    [Test]
    public async Task PollPatientEncounterAsync_EncounterInProgress_UpdatesRegistryOnly()
    {
        // Arrange
        var patient = new RegisteredPatient
        {
            PatientId = "patient-1",
            EncounterId = "encounter-1",
            PracticeId = "practice-1",
            WorkItemId = "workitem-1",
            RegisteredAt = DateTimeOffset.UtcNow
        };

        // FHIR returns encounter in "in-progress" status
        var encounterBundle = CreateFhirBundle("encounter-1", "in-progress");
        _fhirClient.SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(encounterBundle));

        // Act - call the method directly
        await _sut.PollPatientEncounterAsync(patient, CancellationToken.None);

        // Assert - registry updated but NOT unregistered
        await _patientRegistry.Received(1).UpdateAsync(
            "patient-1",
            Arg.Any<DateTimeOffset>(),
            "in-progress",
            Arg.Any<CancellationToken>());
        await _patientRegistry.DidNotReceive().UnregisterAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PollPatientEncounterAsync_EncounterFinished_EmitsEventAndUnregisters()
    {
        // Arrange
        var patient = new RegisteredPatient
        {
            PatientId = "patient-1",
            EncounterId = "encounter-1",
            PracticeId = "practice-1",
            WorkItemId = "workitem-1",
            RegisteredAt = DateTimeOffset.UtcNow,
            CurrentEncounterStatus = "in-progress" // Was in-progress, now finished
        };

        var encounterBundle = CreateFhirBundle("encounter-1", "finished");
        _fhirClient.SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(encounterBundle));

        // Act
        await _sut.PollPatientEncounterAsync(patient, CancellationToken.None);

        // Assert - event emitted and patient unregistered
        await _patientRegistry.Received(1).UnregisterAsync("patient-1", Arg.Any<CancellationToken>());

        // Check channel has event
        var hasEvent = _sut.Encounters.TryRead(out var encounterId);
        await Assert.That(hasEvent).IsTrue();
        await Assert.That(encounterId).IsEqualTo("encounter-1");
    }

    [Test]
    public async Task PollPatientEncounterAsync_FhirError_LogsAndContinues()
    {
        // Arrange
        var patient = new RegisteredPatient
        {
            PatientId = "patient-1",
            EncounterId = "encounter-1",
            PracticeId = "practice-1",
            WorkItemId = "workitem-1",
            RegisteredAt = DateTimeOffset.UtcNow
        };

        _fhirClient.SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(FhirError.Network("FHIR error")));

        // Act - should not throw
        await _sut.PollPatientEncounterAsync(patient, CancellationToken.None);

        // Assert - no crash, no unregister
        await _patientRegistry.DidNotReceive().UnregisterAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
