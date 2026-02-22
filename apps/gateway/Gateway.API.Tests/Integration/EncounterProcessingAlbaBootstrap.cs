// =============================================================================
// <copyright file="EncounterProcessingAlbaBootstrap.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using Alba;
using Gateway.API.Contracts;
using Gateway.API.Data;
using Gateway.API.Models;
using Gateway.API.Services.Polling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using StackExchange.Redis;
using TUnit.Core.Interfaces;

namespace Gateway.API.Tests.Integration;

/// <summary>
/// Alba bootstrap for encounter processing integration tests.
/// Provides mocked FHIR services with clinical bundles containing service requests.
/// </summary>
public sealed class EncounterProcessingAlbaBootstrap : IAsyncInitializer, IAsyncDisposable
{
    /// <summary>
    /// Test API key for integration tests.
    /// </summary>
    public const string TestApiKey = "test-api-key";

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
                    // API key configuration
                    ["ApiKey:ValidApiKeys:0"] = TestApiKey,
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
                    ["ClinicalQuery:ProcedureLookbackMonths"] = "24",
                    // Fake connection string for Aspire PostgreSQL component validation
                    // (we override the DbContext with in-memory provider in ConfigureServices)
                    ["ConnectionStrings:authscript"] = "Host=localhost;Database=test;Username=test;Password=test"
                });
            });

            config.ConfigureServices(services =>
            {
                // Replace Aspire's DbContext registration with EF Core in-memory provider
                // Remove all EF Core-related registrations that Aspire created
                // Note: We need to remove ALL registrations related to the DbContext to avoid
                // the Aspire component's factory being invoked during migration
                var dbContextDescriptors = services.Where(d =>
                    d.ServiceType == typeof(GatewayDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<GatewayDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.FullName?.Contains("GatewayDbContext") == true ||
                    d.ImplementationType?.FullName?.Contains("GatewayDbContext") == true).ToList();
                foreach (var descriptor in dbContextDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IDbContextPool<GatewayDbContext>));
                services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IScopedDbContextLease<GatewayDbContext>));

                // Use a fixed database name to ensure data persists across scopes within the same test
                var databaseName = $"GatewayTest_{Guid.NewGuid()}";
                services.AddDbContext<GatewayDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));

                // Remove MigrationService (in-memory DB doesn't need migration)
                var migrationServiceDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType?.IsGenericType == true &&
                    d.ImplementationType.GetGenericTypeDefinition() == typeof(MigrationService<>));
                if (migrationServiceDescriptor != null)
                {
                    services.Remove(migrationServiceDescriptor);
                }

                // Mark migrations as complete so MigrationGateMiddleware passes requests through
                MigrationHealthCheck.RegisterExpected(nameof(GatewayDbContext));
                MigrationHealthCheck.MarkComplete(nameof(GatewayDbContext));

                // Remove AthenaPollingService to avoid scoped dependency validation issue
                // The singleton background service cannot consume scoped IPatientRegistry
                services.RemoveAll<AthenaPollingService>();

                // Also remove the IHostedService registration for AthenaPollingService
                var hostedServiceDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationFactory?.Method.ReturnType == typeof(AthenaPollingService));
                if (hostedServiceDescriptor != null)
                {
                    services.Remove(hostedServiceDescriptor);
                }

                // Replace IFhirTokenProvider with mock returning test token
                var mockTokenProvider = Substitute.For<IFhirTokenProvider>();
                mockTokenProvider
                    .GetTokenAsync(Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult("test-access-token"));
                services.RemoveAll<IFhirTokenProvider>();
                services.AddSingleton(mockTokenProvider);

                // Replace IFhirClient with mock to avoid real FHIR calls
                services.RemoveAll<IFhirClient>();
                services.AddSingleton(Substitute.For<IFhirClient>());

                // Replace IFhirDataAggregator with mock returning clinical bundle WITH service requests
                var mockAggregator = Substitute.For<IFhirDataAggregator>();
                mockAggregator
                    .AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(CreateClinicalBundleWithServiceRequest()));
                services.RemoveAll<IFhirDataAggregator>();
                services.AddSingleton(mockAggregator);

                // Replace IIntelligenceClient with mock returning test PA form data
                var mockIntelligenceClient = Substitute.For<IIntelligenceClient>();
                mockIntelligenceClient
                    .AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(call => Task.FromResult(new PAFormData
                    {
                        PatientName = "Test Patient",
                        PatientDob = "1990-01-01",
                        MemberId = "TEST-001",
                        DiagnosisCodes = ["M54.5"],
                        ProcedureCode = call.ArgAt<string>(1), // echo back the requested procedure code
                        ClinicalSummary = "Test clinical summary for integration tests.",
                        SupportingEvidence =
                        [
                            new EvidenceItem
                            {
                                CriterionId = "crit-1",
                                CriterionLabel = "Test criterion",
                                Status = "MET",
                                Evidence = "Test evidence",
                                Source = "Test",
                                Confidence = 0.9,
                            }
                        ],
                        Recommendation = "APPROVE",
                        ConfidenceScore = 0.85,
                        FieldMappings = new Dictionary<string, string>(),
                    }));
                services.RemoveAll<IIntelligenceClient>();
                services.AddSingleton(mockIntelligenceClient);

                // PdfFormStamper is already a stub - no replacement needed

                // Remove the Aspire Redis registration that would fail without a real connection
                // and provide a null IConnectionMultiplexer so AnalysisResultStore skips Redis storage
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddSingleton<IConnectionMultiplexer?>(sp => null);
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

    private static ClinicalBundle CreateClinicalBundleWithServiceRequest()
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
            Conditions =
            [
                new ConditionInfo
                {
                    Id = "test-condition-001",
                    Code = "M54.5",
                    Display = "Low back pain",
                    ClinicalStatus = "active",
                    OnsetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6))
                }
            ],
            Observations =
            [
                new ObservationInfo
                {
                    Id = "test-observation-001",
                    Code = "59408-5",
                    Display = "Oxygen saturation",
                    Value = "98%",
                    EffectiveDate = DateTimeOffset.UtcNow.AddDays(-1)
                }
            ],
            Procedures =
            [
                new ProcedureInfo
                {
                    Id = "test-procedure-001",
                    Code = "97110",
                    Display = "Therapeutic exercises",
                    PerformedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3))
                }
            ],
            Documents = [],
            ServiceRequests =
            [
                new ServiceRequestInfo
                {
                    Id = "test-service-request-001",
                    Status = "active",
                    Code = new CodeableConcept
                    {
                        Coding =
                        [
                            new Coding
                            {
                                System = "http://www.ama-assn.org/go/cpt",
                                Code = "72148",
                                Display = "MRI lumbar spine without contrast"
                            }
                        ],
                        Text = "MRI lumbar spine without contrast"
                    },
                    EncounterId = "test-encounter",
                    AuthoredOn = DateTimeOffset.UtcNow
                }
            ]
        };
    }
}
