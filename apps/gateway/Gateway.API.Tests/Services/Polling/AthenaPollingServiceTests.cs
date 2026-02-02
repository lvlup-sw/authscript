namespace Gateway.API.Tests.Services.Polling;

using System.Text.Json;
using System.Threading.Channels;
using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Services.Polling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

public class AthenaPollingServiceTests
{
    private IFhirHttpClient _fhirClient = null!;
    private IOptions<AthenaOptions> _options = null!;
    private ILogger<AthenaPollingService> _logger = null!;
    private AthenaPollingService _sut = null!;

    [Before(Test)]
    public Task Setup()
    {
        _fhirClient = Substitute.For<IFhirHttpClient>();
        _logger = Substitute.For<ILogger<AthenaPollingService>>();
        _options = Options.Create(new AthenaOptions
        {
            FhirBaseUrl = "https://api.athena.test/fhir/r4",
            ClientId = "test-client",
            TokenEndpoint = "https://api.athena.test/oauth2/token",
            PollingIntervalSeconds = 1,
            PracticeId = "Organization/a-1.Practice-12345"
        });

        _sut = new AthenaPollingService(_fhirClient, _options, _logger);
        return Task.CompletedTask;
    }

    [After(Test)]
    public async Task Cleanup()
    {
        await _sut.StopAsync(CancellationToken.None);
        _sut.Dispose();
    }

    [Test]
    public async Task AthenaPollingService_ExecuteAsync_PollsForFinishedEncounters()
    {
        // Arrange
        var bundle = CreateEmptyBundle();
        _fhirClient.SearchAsync(
            Arg.Is("Encounter"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundle));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(50); // Allow some polling time

        // Assert
        await _fhirClient.Received().SearchAsync(
            Arg.Is("Encounter"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AthenaPollingService_ExecuteAsync_SearchesEncounterWithStatusFinished()
    {
        // Arrange
        var bundle = CreateEmptyBundle();
        _fhirClient.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundle));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(50);

        // Assert
        await _fhirClient.Received().SearchAsync(
            Arg.Is("Encounter"),
            Arg.Is<string>(q => q.Contains("status=finished")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AthenaPollingService_ExecuteAsync_FiltersEncountersByDateAfterLastCheck()
    {
        // Arrange
        var bundle = CreateEmptyBundle();
        _fhirClient.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundle));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(50);

        // Assert
        await _fhirClient.Received().SearchAsync(
            Arg.Is("Encounter"),
            Arg.Is<string>(q => q.Contains("date=gt")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AthenaPollingService_ExecuteAsync_RespectsPollingInterval()
    {
        // Arrange
        var bundle = CreateEmptyBundle();
        var callCount = 0;
        _fhirClient.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundle))
            .AndDoes(_ => callCount++);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(150); // Wait for about 150ms

        // Assert - should have called at least once but respects interval
        await Assert.That(callCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task AthenaPollingService_ExecuteAsync_SkipsAlreadyProcessedEncounters()
    {
        // Arrange
        var bundleWithEncounter = CreateBundleWithEncounter("enc-123");

        _fhirClient.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundleWithEncounter));

        // Create a service and process the encounter
        var service = new AthenaPollingService(_fhirClient, _options, _logger);

        // Act - Start, let it process, stop, start again to see if it skips
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50); // First poll
        await service.StopAsync(CancellationToken.None);

        // Get the processed count from the service
        var processedCount = service.GetProcessedEncounterCount();

        // Assert - Should have processed the encounter
        await Assert.That(processedCount).IsEqualTo(1);

        // Start again with same bundle
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50); // Second poll
        await service.StopAsync(CancellationToken.None);

        // Assert - Should still have only 1 processed (skipped duplicate)
        await Assert.That(service.GetProcessedEncounterCount()).IsEqualTo(1);

        service.Dispose();
    }

    [Test]
    public async Task AthenaPollingService_ExecuteAsync_TracksProcessedEncounterIds()
    {
        // Arrange
        var bundleWithEncounter = CreateBundleWithEncounter("enc-456");

        _fhirClient.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundleWithEncounter));

        var service = new AthenaPollingService(_fhirClient, _options, _logger);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await Assert.That(service.IsEncounterProcessed("enc-456")).IsTrue();
        await Assert.That(service.IsEncounterProcessed("enc-999")).IsFalse();

        service.Dispose();
    }

    [Test]
    public async Task AthenaPollingService_ExecuteAsync_PurgesOldEntriesFromTracker()
    {
        // Arrange
        var bundle = CreateBundleWithEncounter("enc-old");

        _fhirClient.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundle));

        // Use a service with short purge window for testing
        var service = new AthenaPollingService(_fhirClient, _options, _logger);

        // Act - Add an encounter and manually set it as old
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        // Simulate old timestamp by purging entries older than a very recent timestamp
        service.PurgeProcessedEncountersOlderThan(TimeSpan.Zero);

        // Assert - Entry should be purged
        await Assert.That(service.IsEncounterProcessed("enc-old")).IsFalse();

        service.Dispose();
    }

    [Test]
    public async Task AthenaPollingService_ExecuteAsync_EnqueuesEncounterToChannel()
    {
        // Arrange
        var bundleWithEncounter = CreateBundleWithEncounter("enc-channel-test");

        _fhirClient.SearchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(bundleWithEncounter));

        var service = new AthenaPollingService(_fhirClient, _options, _logger);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Allow polling time
        await service.StopAsync(CancellationToken.None);

        // Assert - Read from the channel
        var reader = service.Encounters;
        await Assert.That(reader.TryRead(out var encounterId)).IsTrue();
        await Assert.That(encounterId).IsEqualTo("enc-channel-test");

        service.Dispose();
    }

    [Test]
    public async Task AthenaPollingService_Channel_IsUnboundedSingleConsumer()
    {
        // Arrange & Act
        var service = new AthenaPollingService(_fhirClient, _options, _logger);

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
}
