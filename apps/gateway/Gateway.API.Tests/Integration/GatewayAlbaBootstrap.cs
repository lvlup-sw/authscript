using Alba;
using Gateway.API.Contracts;
using Gateway.API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using TUnit.Core.Interfaces;

namespace Gateway.API.Tests.Integration;

/// <summary>
/// Alba bootstrap for Gateway integration tests.
/// Uses TUnit's IAsyncInitializer for async initialization.
/// </summary>
public sealed class GatewayAlbaBootstrap : IAsyncInitializer, IAsyncDisposable
{
    /// <summary>
    /// Gets the Alba host for making HTTP requests.
    /// </summary>
    public IAlbaHost Host { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        Host = await AlbaHost.For<Program>(config =>
        {
            // Add test configuration to satisfy required options
            config.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Athena configuration (required by AthenaOptions.IsValid())
                    ["Athena:ClientId"] = "test-client-id",
                    ["Athena:ClientSecret"] = "test-client-secret",
                    ["Athena:FhirBaseUrl"] = "https://api.test.athenahealth.com/fhir/r4",
                    ["Athena:TokenEndpoint"] = "https://api.test.athenahealth.com/oauth2/v1/token",
                    ["Athena:PollingIntervalSeconds"] = "30",
                    // Intelligence configuration
                    ["Intelligence:BaseUrl"] = "http://localhost:8000",
                    ["Intelligence:TimeoutSeconds"] = "30",
                    // Clinical query configuration
                    ["ClinicalQuery:ObservationLookbackMonths"] = "12",
                    ["ClinicalQuery:ProcedureLookbackMonths"] = "24"
                });
            });

            config.ConfigureServices(services =>
            {
                // Replace IFhirClient with mock to avoid real FHIR calls
                services.RemoveAll<IFhirClient>();
                services.AddSingleton(Substitute.For<IFhirClient>());

                // Replace IFhirDataAggregator with mock returning empty clinical bundle
                var mockAggregator = Substitute.For<IFhirDataAggregator>();
                mockAggregator
                    .AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(CreateEmptyClinicalBundle()));
                services.RemoveAll<IFhirDataAggregator>();
                services.AddSingleton(mockAggregator);

                // IntelligenceClient is already a stub - no replacement needed
            });
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Host != null)
        {
            await Host.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static ClinicalBundle CreateEmptyClinicalBundle()
    {
        return new ClinicalBundle
        {
            PatientId = "test-patient",
            Patient = new PatientInfo
            {
                Id = "test-patient",
                GivenName = "Test",
                FamilyName = "Patient",
                BirthDate = new DateOnly(1990, 1, 1),
                Gender = "male"
            },
            Conditions = [],
            Observations = [],
            Procedures = [],
            Documents = []
        };
    }
}
