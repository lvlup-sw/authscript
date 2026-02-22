// =============================================================================
// <copyright file="PARequestDataSeeder.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

using System.Text.Json;
using Gateway.API.Data.Entities;
using Gateway.API.GraphQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Gateway.API.Data;

/// <summary>
/// Seeds demo prior authorization requests for development environments.
/// Idempotent: skips seeding if any PA requests already exist.
/// </summary>
public sealed class PARequestDataSeeder : IDataSeeder<GatewayDbContext>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ILogger<PARequestDataSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PARequestDataSeeder"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PARequestDataSeeder(ILogger<PARequestDataSeeder> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAsync(GatewayDbContext context, CancellationToken cancellationToken)
    {
        if (await context.PriorAuthRequests.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("PA requests already exist, skipping seed.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var entities = CreateDemoEntities(now);

        context.PriorAuthRequests.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} demo PA requests.", entities.Length);
    }

    internal static PriorAuthRequestEntity[] CreateDemoEntities(DateTimeOffset now)
    {
        return
        [
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-001",
                PatientId = "60178",
                FhirPatientId = "a-195900.E-60178",
                PatientName = "Sarah Johnson",
                PatientMrn = "MRN-10001",
                PatientDob = "March 15, 1985",
                PatientMemberId = "BCBS-100234",
                PatientPayer = "Blue Cross Blue Shield",
                PatientAddress = "123 Main St, Springfield, IL 62701",
                PatientPhone = "(555) 123-4567",
                ProcedureCode = "72148",
                ProcedureName = "MRI Lumbar Spine w/o Contrast",
                DiagnosisCode = "M54.5",
                DiagnosisName = "Low Back Pain",
                ProviderId = "DR001",
                ProviderName = "Dr. Amanda Martinez",
                ProviderNpi = "1234567890",
                ServiceDate = now.ToString("MMMM d, yyyy"),
                PlaceOfService = "Outpatient",
                ClinicalSummary = "Patient presents with chronic low back pain for 6 months, unresponsive to conservative treatment including physical therapy and NSAIDs. MRI recommended to evaluate for disc herniation or spinal stenosis.",
                Status = "ready",
                Confidence = 87,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = true, Label = "Conservative treatment failed", Reason = "6 months of PT and NSAIDs without improvement" },
                    new CriterionModel { Met = true, Label = "Clinical indication present", Reason = "Chronic low back pain with radiculopathy symptoms" },
                    new CriterionModel { Met = true, Label = "Prior imaging reviewed", Reason = "X-ray completed showing degenerative changes" },
                    new CriterionModel { Met = false, Label = "Specialist referral", Reason = "No specialist referral documented" },
                ]),
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now.AddMinutes(-30),
                ReadyAt = now.AddMinutes(-30),
            },
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-002",
                PatientId = "60179",
                FhirPatientId = "a-195900.E-60179",
                PatientName = "James Wilson",
                PatientMrn = "MRN-10002",
                PatientDob = "July 22, 1958",
                PatientMemberId = "AET-200567",
                PatientPayer = "Aetna",
                PatientAddress = "456 Oak Ave, Chicago, IL 60601",
                PatientPhone = "(555) 234-5678",
                ProcedureCode = "27447",
                ProcedureName = "Total Knee Replacement",
                DiagnosisCode = "M17.11",
                DiagnosisName = "Primary Osteoarthritis, Right Knee",
                ProviderId = "DR002",
                ProviderName = "Dr. Robert Kim",
                ProviderNpi = "0987654321",
                ServiceDate = now.AddDays(14).ToString("MMMM d, yyyy"),
                PlaceOfService = "Inpatient",
                ClinicalSummary = "67-year-old male with severe right knee osteoarthritis. BMI 28. Failed 12 months of conservative management including corticosteroid injections, physical therapy, and bracing. Kellgren-Lawrence Grade IV on imaging.",
                Status = "ready",
                Confidence = 94,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = true, Label = "Conservative treatment exhausted", Reason = "12 months of PT, injections, and bracing" },
                    new CriterionModel { Met = true, Label = "Imaging confirms severity", Reason = "KL Grade IV osteoarthritis on X-ray" },
                    new CriterionModel { Met = true, Label = "Functional limitation", Reason = "Unable to walk more than 1 block, difficulty with stairs" },
                    new CriterionModel { Met = true, Label = "BMI within range", Reason = "BMI 28, within surgical criteria" },
                ]),
                CreatedAt = now.AddHours(-4),
                UpdatedAt = now.AddHours(-1),
                ReadyAt = now.AddHours(-1),
            },
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-003",
                PatientId = "60180",
                FhirPatientId = "a-195900.E-60180",
                PatientName = "Maria Garcia",
                PatientMrn = "MRN-10003",
                PatientDob = "November 8, 1972",
                PatientMemberId = "UHC-300891",
                PatientPayer = "United Healthcare",
                PatientAddress = "789 Pine Rd, Austin, TX 78701",
                PatientPhone = "(555) 345-6789",
                ProcedureCode = "J1745",
                ProcedureName = "Infliximab (Remicade)",
                DiagnosisCode = "M06.9",
                DiagnosisName = "Rheumatoid Arthritis, Unspecified",
                ProviderId = "DR001",
                ProviderName = "Dr. Amanda Martinez",
                ProviderNpi = "1234567890",
                ServiceDate = now.AddDays(7).ToString("MMMM d, yyyy"),
                PlaceOfService = "Outpatient",
                ClinicalSummary = "Patient with moderate-to-severe rheumatoid arthritis, failed methotrexate and hydroxychloroquine. DAS28 score 5.1 indicating high disease activity. Requesting biologic therapy initiation.",
                Status = "draft",
                Confidence = 0,
                CreatedAt = now.AddMinutes(-45),
                UpdatedAt = now.AddMinutes(-45),
            },
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-004",
                PatientId = "60181",
                FhirPatientId = "a-195900.E-60181",
                PatientName = "Robert Chen",
                PatientMrn = "MRN-10004",
                PatientDob = "February 3, 1990",
                PatientMemberId = "CIG-400123",
                PatientPayer = "Cigna",
                PatientAddress = "321 Elm St, Seattle, WA 98101",
                PatientPhone = "(555) 456-7890",
                ProcedureCode = "70553",
                ProcedureName = "MRI Brain w/ & w/o Contrast",
                DiagnosisCode = "G43.909",
                DiagnosisName = "Migraine, Unspecified",
                ProviderId = "DR003",
                ProviderName = "Dr. Lisa Thompson",
                ProviderNpi = "1122334455",
                ServiceDate = now.AddDays(3).ToString("MMMM d, yyyy"),
                PlaceOfService = "Outpatient",
                ClinicalSummary = "35-year-old male with new-onset severe migraines with aura, increasing in frequency over 3 months. Neurological exam notable for mild papilledema. MRI brain indicated to rule out intracranial pathology.",
                Status = "submitted",
                Confidence = 91,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = true, Label = "New neurological symptoms", Reason = "New-onset migraines with aura and papilledema" },
                    new CriterionModel { Met = true, Label = "Red flag symptoms", Reason = "Papilledema on exam suggests elevated intracranial pressure" },
                    new CriterionModel { Met = true, Label = "Progressive symptoms", Reason = "Increasing frequency over 3 months" },
                ]),
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddHours(-6),
                ReadyAt = now.AddHours(-12),
                SubmittedAt = now.AddHours(-6),
                ReviewTimeSeconds = 142,
            },
        ];
    }

    private static string SerializeCriteria(List<CriterionModel> criteria) =>
        JsonSerializer.Serialize(criteria, JsonOptions);
}
