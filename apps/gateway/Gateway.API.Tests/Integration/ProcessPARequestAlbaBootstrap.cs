// =============================================================================
// <copyright file="ProcessPARequestAlbaBootstrap.cs" company="Levelup Software">
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
/// Alba bootstrap for ProcessPARequest integration tests.
/// Provides mocked FHIR aggregator and Intelligence client to exercise the full
/// GraphQL mutation through the ASP.NET pipeline.
/// </summary>
public sealed class ProcessPARequestAlbaBootstrap : IAsyncInitializer, IAsyncDisposable
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
            config.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiKey:ValidApiKeys:0"] = TestApiKey,
                    ["Athena:ClientId"] = "test-client-id",
                    ["Athena:ClientSecret"] = "test-client-secret",
                    ["Athena:FhirBaseUrl"] = "https://api.test.athenahealth.com/fhir/r4",
                    ["Athena:TokenEndpoint"] = "https://api.test.athenahealth.com/oauth2/v1/token",
                    ["Athena:PollingIntervalSeconds"] = "30",
                    ["Intelligence:BaseUrl"] = "http://localhost:8000",
                    ["Intelligence:TimeoutSeconds"] = "30",
                    ["ClinicalQuery:ObservationLookbackMonths"] = "12",
                    ["ClinicalQuery:ProcedureLookbackMonths"] = "24",
                    ["ConnectionStrings:authscript"] = "Host=localhost;Database=test;Username=test;Password=test"
                });
            });

            config.ConfigureServices(services =>
            {
                // Replace Aspire's DbContext with in-memory provider
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

                var databaseName = $"GatewayTest_{Guid.NewGuid()}";
                services.AddDbContext<GatewayDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));

                // Remove AthenaPollingService
                services.RemoveAll<AthenaPollingService>();
                var hostedServiceDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationFactory?.Method.ReturnType == typeof(AthenaPollingService));
                if (hostedServiceDescriptor != null)
                {
                    services.Remove(hostedServiceDescriptor);
                }

                // Mock IFhirTokenProvider
                var mockTokenProvider = Substitute.For<IFhirTokenProvider>();
                mockTokenProvider
                    .GetTokenAsync(Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult("test-access-token"));
                services.RemoveAll<IFhirTokenProvider>();
                services.AddSingleton(mockTokenProvider);

                // Mock IFhirClient
                services.RemoveAll<IFhirClient>();
                services.AddSingleton(Substitute.For<IFhirClient>());

                // Mock IFhirDataAggregator with clinical data for patient "60178" (Donna Sandbox)
                var mockAggregator = Substitute.For<IFhirDataAggregator>();
                mockAggregator
                    .AggregateClinicalDataAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(CreateTestClinicalBundle()));
                services.RemoveAll<IFhirDataAggregator>();
                services.AddSingleton(mockAggregator);

                // Mock IIntelligenceClient with realistic PA form data
                var mockIntelligenceClient = Substitute.For<IIntelligenceClient>();
                mockIntelligenceClient
                    .AnalyzeAsync(Arg.Any<ClinicalBundle>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(CreateTestPAFormData()));
                services.RemoveAll<IIntelligenceClient>();
                services.AddSingleton(mockIntelligenceClient);

                // Remove Redis (not available in tests)
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

    private static ClinicalBundle CreateTestClinicalBundle()
    {
        return new ClinicalBundle
        {
            PatientId = "60178",
            Patient = new PatientInfo
            {
                Id = "60178",
                GivenName = "Donna",
                FamilyName = "Sandbox",
                BirthDate = new DateOnly(1968, 3, 15),
                Gender = "female",
                MemberId = "ATH60178"
            },
            Conditions =
            [
                new ConditionInfo
                {
                    Id = "cond-001",
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
                    Id = "obs-001",
                    Code = "72166-2",
                    Display = "Smoking status",
                    Value = "Never smoker"
                }
            ],
            Procedures =
            [
                new ProcedureInfo
                {
                    Id = "proc-001",
                    Code = "97110",
                    Display = "Therapeutic exercises",
                    PerformedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2))
                }
            ],
            Documents = [],
            ServiceRequests = []
        };
    }

    private static PAFormData CreateTestPAFormData() => new()
    {
        PatientName = "Donna Sandbox",
        PatientDob = "1968-03-15",
        MemberId = "ATH60178",
        DiagnosisCodes = ["M54.5"],
        ProcedureCode = "72148",
        ClinicalSummary = "Patient presents with chronic low back pain. Conservative therapy including 8 weeks of physical therapy and NSAID therapy has been attempted without adequate relief. MRI lumbar spine is medically indicated.",
        SupportingEvidence =
        [
            new EvidenceItem
            {
                CriterionId = "conservative_therapy",
                Status = "MET",
                Evidence = "8 weeks of physical therapy and NSAID therapy documented",
                Source = "Clinical Notes",
                Confidence = 0.95
            },
            new EvidenceItem
            {
                CriterionId = "failed_treatment",
                Status = "MET",
                Evidence = "Persistent pain rated 7/10 despite conservative therapy",
                Source = "Progress Notes",
                Confidence = 0.90
            },
            new EvidenceItem
            {
                CriterionId = "diagnosis_present",
                Status = "MET",
                Evidence = "Valid ICD-10 code M54.5 (Low Back Pain) documented",
                Source = "Problem List",
                Confidence = 0.99
            }
        ],
        Recommendation = "APPROVE",
        ConfidenceScore = 0.92,
        FieldMappings = new Dictionary<string, string>
        {
            ["PatientName"] = "Donna Sandbox",
            ["PatientDOB"] = "1968-03-15",
            ["MemberID"] = "ATH60178",
            ["ProcedureCode"] = "72148"
        }
    };
}
