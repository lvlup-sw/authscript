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
/// Clears all existing PA requests on startup to ensure a clean demo state.
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
        var existingCount = await context.PriorAuthRequests.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            _logger.LogInformation("Clearing {Count} existing PA requests for clean demo state.", existingCount);
            context.PriorAuthRequests.RemoveRange(context.PriorAuthRequests);
            await context.SaveChangesAsync(cancellationToken);
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
            // ---------------------------------------------------------------
            // PA-DEMO-001: Donna Sandboxtest — Echocardiogram (93306)
            // Real FHIR patient 60178. CHF + AFib → echo is standard of care.
            // All criteria MET → APPROVE at 92%.
            // ---------------------------------------------------------------
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-001",
                PatientId = "60178",
                FhirPatientId = "a-195900.E-60178",
                PatientName = "Donna Sandboxtest",
                PatientMrn = "MRN-60178",
                PatientDob = "February 1, 1984",
                PatientMemberId = "BCBS-RI-27700",
                PatientPayer = "Blue Cross Blue Shield of Rhode Island",
                PatientAddress = "42 Benefit St, Providence, RI 02903",
                PatientPhone = "(401) 555-0178",
                ProcedureCode = "93306",
                ProcedureName = "Echocardiogram, Complete",
                DiagnosisCode = "I50.32",
                DiagnosisName = "Chronic Diastolic Heart Failure",
                ProviderId = "DR001",
                ProviderName = "Dr. Kelli Smith",
                ProviderNpi = "1234567890",
                ServiceDate = now.AddDays(2).ToString("MMMM d, yyyy"),
                PlaceOfService = "Outpatient",
                ClinicalSummary = "41-year-old female with documented diastolic heart failure (I50.32) and atrial fibrillation (I48.91) on metoprolol, furosemide, and apixaban. Echocardiogram indicated to assess ejection fraction, chamber dimensions, and valvular function per ACC/AHA guidelines. Comorbidities include essential hypertension, type 2 diabetes, hyperlipidemia, and CKD stage 1. Labs notable for glucose 142 mg/dL, creatinine 0.7 mg/dL.",
                Status = "ready",
                Confidence = 92,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = true, Label = "Medical necessity documented", Reason = "Diastolic heart failure with concurrent AFib requires echocardiographic assessment of cardiac function per ACC/AHA guidelines" },
                    new CriterionModel { Met = true, Label = "Valid diagnosis code present", Reason = "I50.32 (Diastolic HF) and I48.91 (AFib) are established indications for echocardiography" },
                    new CriterionModel { Met = true, Label = "Conservative therapy attempted or N/A", Reason = "Patient on active cardiac management (metoprolol, furosemide, apixaban). Imaging is diagnostic — conservative therapy not applicable" },
                ]),
                CreatedAt = now.AddHours(-1),
                UpdatedAt = now.AddMinutes(-20),
                ReadyAt = now.AddMinutes(-20),
            },

            // ---------------------------------------------------------------
            // PA-DEMO-002: Donna Sandboxtest — MRI Brain (70553)
            // Same patient, different request. AFib + HTN → stroke screening.
            // Two required criteria UNCLEAR → NEED_INFO at 48%.
            // ---------------------------------------------------------------
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-002",
                PatientId = "60178",
                FhirPatientId = "a-195900.E-60178",
                PatientName = "Donna Sandboxtest",
                PatientMrn = "MRN-60178",
                PatientDob = "February 1, 1984",
                PatientMemberId = "BCBS-RI-27700",
                PatientPayer = "Blue Cross Blue Shield of Rhode Island",
                PatientAddress = "42 Benefit St, Providence, RI 02903",
                PatientPhone = "(401) 555-0178",
                ProcedureCode = "70553",
                ProcedureName = "MRI Brain w/ & w/o Contrast",
                DiagnosisCode = "I48.91",
                DiagnosisName = "Unspecified Atrial Fibrillation",
                ProviderId = "DR003",
                ProviderName = "Dr. Lisa Thompson",
                ProviderNpi = "1122334455",
                ServiceDate = now.AddDays(5).ToString("MMMM d, yyyy"),
                PlaceOfService = "Outpatient",
                ClinicalSummary = "41-year-old female with atrial fibrillation and multiple stroke risk factors (HTN, DM, CHF — CHA2DS2-VASc score >= 4). MRI brain requested for stroke risk screening. No acute neurological symptoms or deficits documented. No prior CT performed.",
                Status = "ready",
                Confidence = 48,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = null, Label = "Valid ICD-10 for neurological condition (LCD L37373)", Reason = "I48.91 (AFib) and I10 (HTN) increase stroke risk but are cardiovascular codes, not primary neurological diagnoses" },
                    new CriterionModel { Met = null, Label = "Neurological indication documented", Reason = "High CHA2DS2-VASc score suggests stroke risk, but no acute neurological symptoms, seizures, or deficits are documented" },
                    new CriterionModel { Met = false, Label = "CT insufficient or MRI specifically indicated", Reason = "No prior head CT has been performed" },
                    new CriterionModel { Met = true, Label = "Supporting clinical findings documented", Reason = "Comprehensive vitals, labs, medication list, and allergy profile documented" },
                ]),
                CreatedAt = now.AddHours(-3),
                UpdatedAt = now.AddHours(-2),
                ReadyAt = now.AddHours(-2),
            },

            // ---------------------------------------------------------------
            // PA-DEMO-003: Gary Sandboxtest — Total Knee Arthroplasty (27447)
            // Real FHIR patient 60183. CAD + multiple comorbidities.
            // Knee OA well-documented, but surgical risk high → 94% APPROVE.
            // ---------------------------------------------------------------
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-003",
                PatientId = "60183",
                FhirPatientId = "a-195900.E-60183",
                PatientName = "Gary Sandboxtest",
                PatientMrn = "MRN-60183",
                PatientDob = "April 18, 1948",
                PatientMemberId = "AET-200567",
                PatientPayer = "Aetna",
                PatientAddress = "456 Oak Ave, Chicago, IL 60601",
                PatientPhone = "(312) 555-0183",
                ProcedureCode = "27447",
                ProcedureName = "Total Knee Replacement",
                DiagnosisCode = "M17.11",
                DiagnosisName = "Primary Osteoarthritis, Right Knee",
                ProviderId = "DR002",
                ProviderName = "Dr. Robert Kim",
                ProviderNpi = "0987654321",
                ServiceDate = now.AddDays(21).ToString("MMMM d, yyyy"),
                PlaceOfService = "Inpatient",
                ClinicalSummary = "77-year-old male with severe right knee osteoarthritis and extensive medical history including coronary atherosclerosis (s/p PCI), HTN, hyperlipidemia, gout, depression, and anxiety. Failed 14 months of conservative management: NSAIDs, physical therapy, corticosteroid injections, and bracing. Kellgren-Lawrence Grade IV. Unable to walk > 1 block. Cardiology clearance obtained for surgery.",
                Status = "ready",
                Confidence = 94,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = true, Label = "Valid ICD-10 for knee joint disease (LCD L36575)", Reason = "M17.11 (Primary osteoarthritis, right knee) is a covered diagnosis" },
                    new CriterionModel { Met = true, Label = "Advanced joint disease on imaging", Reason = "Kellgren-Lawrence Grade IV: complete joint space loss, large osteophytes, subchondral sclerosis" },
                    new CriterionModel { Met = true, Label = "Functional impairment interfering with ADLs", Reason = "Unable to walk more than 1 block, difficulty with stairs, requires assistive device for ambulation" },
                    new CriterionModel { Met = true, Label = "Failed conservative management", Reason = "14 months of NSAIDs, PT (24 sessions), 3 corticosteroid injections, knee brace — no sustained relief" },
                    new CriterionModel { Met = true, Label = "No surgical contraindication", Reason = "Cardiology clearance obtained. No active infection. CAD stable post-PCI" },
                ]),
                CreatedAt = now.AddHours(-6),
                UpdatedAt = now.AddHours(-2),
                ReadyAt = now.AddHours(-2),
            },

            // ---------------------------------------------------------------
            // PA-DEMO-004: Rebecca Sandbox-Test — MRI Lumbar Spine (72148)
            // Real FHIR patient 60182. Classic back pain with red flags.
            // Red flag bypasses conservative therapy → 88% APPROVE.
            // ---------------------------------------------------------------
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-004",
                PatientId = "60182",
                FhirPatientId = "a-195900.E-60182",
                PatientName = "Rebecca Sandbox-Test",
                PatientMrn = "MRN-60182",
                PatientDob = "March 10, 1990",
                PatientMemberId = "UHC-300891",
                PatientPayer = "United Healthcare",
                PatientAddress = "789 Pine Rd, Austin, TX 78701",
                PatientPhone = "(512) 555-0182",
                ProcedureCode = "72148",
                ProcedureName = "MRI Lumbar Spine w/o Contrast",
                DiagnosisCode = "M54.5",
                DiagnosisName = "Low Back Pain",
                ProviderId = "DR001",
                ProviderName = "Dr. Kelli Smith",
                ProviderNpi = "1234567890",
                ServiceDate = now.AddDays(3).ToString("MMMM d, yyyy"),
                PlaceOfService = "Outpatient",
                ClinicalSummary = "35-year-old female with acute low back pain and progressive left lower extremity weakness over 2 weeks. Exam reveals L5 dermatomal weakness (4/5 dorsiflexion), diminished ankle reflex, and positive straight-leg raise at 30 degrees. Red flag: progressive neurological deficit warrants urgent MRI to rule out disc herniation with cord/root compression.",
                Status = "ready",
                Confidence = 88,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = true, Label = "Valid ICD-10 for lumbar pathology (LCD L34220)", Reason = "M54.5 (Low back pain) is a covered diagnosis code" },
                    new CriterionModel { Met = true, Label = "Red flag screening positive", Reason = "Progressive neurological deficit: L5 weakness, diminished reflexes. Bypasses conservative therapy requirement per LCD" },
                    new CriterionModel { Met = true, Label = "Conservative therapy 4+ weeks (bypassed)", Reason = "Bypassed — red flag criterion met. Progressive motor deficit warrants urgent imaging without waiting period" },
                    new CriterionModel { Met = true, Label = "Clinical rationale documented", Reason = "Documented neurological exam findings with dermatomal pattern. Clinical suspicion for disc herniation with nerve root compression" },
                    new CriterionModel { Met = true, Label = "No duplicate imaging", Reason = "No prior lumbar MRI or CT on file" },
                ]),
                CreatedAt = now.AddHours(-4),
                UpdatedAt = now.AddHours(-1),
                ReadyAt = now.AddHours(-1),
            },

            // ---------------------------------------------------------------
            // PA-DEMO-005: Eleana Sandboxtest — Omalizumab/Xolair (J2357)
            // Real FHIR patient 60179 (pediatric, age 11). Severe asthma.
            // Draft status — awaiting AI processing. Demonstrates the queue.
            // ---------------------------------------------------------------
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-005",
                PatientId = "60179",
                FhirPatientId = "a-195900.E-60179",
                PatientName = "Eleana Sandboxtest",
                PatientMrn = "MRN-60179",
                PatientDob = "February 27, 2015",
                PatientMemberId = "CIG-400456",
                PatientPayer = "Cigna",
                PatientAddress = "321 Elm St, Seattle, WA 98101",
                PatientPhone = "(206) 555-0179",
                ProcedureCode = "J2357",
                ProcedureName = "Omalizumab (Xolair), 150mg SC",
                DiagnosisCode = "J45.50",
                DiagnosisName = "Severe Persistent Asthma, Uncomplicated",
                ProviderId = "DR003",
                ProviderName = "Dr. Lisa Thompson",
                ProviderNpi = "1122334455",
                ServiceDate = now.AddDays(10).ToString("MMMM d, yyyy"),
                PlaceOfService = "Outpatient",
                ClinicalSummary = "11-year-old female with severe persistent asthma uncontrolled on high-dose ICS/LABA combination and leukotriene modifier. Elevated IgE (450 IU/mL). Three ER visits in past 6 months for exacerbations. Allergist recommends Xolair initiation.",
                Status = "draft",
                Confidence = 0,
                CreatedAt = now.AddMinutes(-30),
                UpdatedAt = now.AddMinutes(-30),
            },

            // ---------------------------------------------------------------
            // PA-DEMO-006: Dorrie Sandboxtest — Total Hip Replacement (27130)
            // Real FHIR patient 60184. Elderly, submitted and approved.
            // Demonstrates completed workflow with review time tracking.
            // ---------------------------------------------------------------
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-006",
                PatientId = "60184",
                FhirPatientId = "a-195900.E-60184",
                PatientName = "Dorrie Sandboxtest",
                PatientMrn = "MRN-60184",
                PatientDob = "November 23, 1949",
                PatientMemberId = "MCD-500789",
                PatientPayer = "Medicare",
                PatientAddress = "100 Federal St, Boston, MA 02110",
                PatientPhone = "(617) 555-0184",
                ProcedureCode = "27130",
                ProcedureName = "Total Hip Replacement",
                DiagnosisCode = "M16.11",
                DiagnosisName = "Primary Osteoarthritis, Right Hip",
                ProviderId = "DR002",
                ProviderName = "Dr. Robert Kim",
                ProviderNpi = "0987654321",
                ServiceDate = now.AddDays(28).ToString("MMMM d, yyyy"),
                PlaceOfService = "Inpatient",
                ClinicalSummary = "76-year-old female with severe right hip osteoarthritis and progressive decline in mobility. Failed 18 months of conservative management including PT, NSAIDs, and two intra-articular corticosteroid injections. X-ray shows Kellgren-Lawrence Grade IV with complete joint space narrowing. BMI 26. Medical clearance obtained.",
                Status = "submitted",
                Confidence = 96,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = true, Label = "Medical necessity documented", Reason = "Severe hip OA with progressive functional decline despite 18 months of conservative management" },
                    new CriterionModel { Met = true, Label = "Valid diagnosis code present", Reason = "M16.11 (Primary osteoarthritis, right hip) is a standard indication for total hip arthroplasty" },
                    new CriterionModel { Met = true, Label = "Conservative therapy exhausted", Reason = "18 months of PT, NSAIDs, and 2 corticosteroid injections without sustained improvement" },
                ]),
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddHours(-8),
                ReadyAt = now.AddDays(-1),
                SubmittedAt = now.AddHours(-8),
                ReviewTimeSeconds = 98,
            },

            // ---------------------------------------------------------------
            // PA-DEMO-007: Anna Testpt — Psychotherapy (90834)
            // Real FHIR patient 60181. Mental health PA request.
            // Missing documentation → MANUAL_REVIEW at 62%.
            // ---------------------------------------------------------------
            new PriorAuthRequestEntity
            {
                Id = "PA-DEMO-007",
                PatientId = "60181",
                FhirPatientId = "a-195900.E-60181",
                PatientName = "Anna Testpt",
                PatientMrn = "MRN-60181",
                PatientDob = "December 17, 1995",
                PatientMemberId = "AET-200891",
                PatientPayer = "Aetna",
                PatientAddress = "55 Water St, New York, NY 10004",
                PatientPhone = "(212) 555-0181",
                ProcedureCode = "90834",
                ProcedureName = "Psychotherapy, 45 minutes",
                DiagnosisCode = "F33.1",
                DiagnosisName = "Major Depressive Disorder, Recurrent, Moderate",
                ProviderId = "DR004",
                ProviderName = "Dr. Sarah Mitchell",
                ProviderNpi = "5566778899",
                ServiceDate = now.AddDays(1).ToString("MMMM d, yyyy"),
                PlaceOfService = "Outpatient",
                ClinicalSummary = "30-year-old female with recurrent moderate MDD requesting continuation of weekly psychotherapy. PHQ-9 score 14 (moderate). Currently on sertraline 100mg. Patient reports partial response to medication alone and requests combined treatment approach.",
                Status = "ready",
                Confidence = 62,
                CriteriaJson = SerializeCriteria(
                [
                    new CriterionModel { Met = true, Label = "Medical necessity documented", Reason = "Recurrent moderate MDD (F33.1) with PHQ-9 of 14. Combined medication + therapy approach supported by evidence" },
                    new CriterionModel { Met = true, Label = "Valid diagnosis code present", Reason = "F33.1 (Major depressive disorder, recurrent, moderate) supports psychotherapy authorization" },
                    new CriterionModel { Met = null, Label = "Treatment plan with measurable goals", Reason = "No formal treatment plan with specific goals, session frequency, or expected duration documented" },
                ]),
                CreatedAt = now.AddHours(-5),
                UpdatedAt = now.AddHours(-3),
                ReadyAt = now.AddHours(-3),
            },
        ];
    }

    private static string SerializeCriteria(List<CriterionModel> criteria) =>
        JsonSerializer.Serialize(criteria, JsonOptions);
}
