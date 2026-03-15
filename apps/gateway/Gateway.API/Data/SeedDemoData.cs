// =============================================================================
// <copyright file="SeedDemoData.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Data;

using Gateway.API.GraphQL.Models;

/// <summary>
/// Generates deterministic demo seed data for the PA dashboard.
/// Produces 48 <see cref="PARequestModel"/> instances with realistic clinical content
/// distributed across the 7 Athena sandbox test patients.
/// </summary>
public static class SeedDemoData
{
    // ── Patient fixtures ────────────────────────────────────────────────────
    private static readonly (string Id, string FhirId, string Name, string Mrn, string Dob, string MemberId, string Payer, string Address, string Phone)[] Patients =
    [
        ("60178", "a-195900.E-60178", "Donna Sandboxtest",   "MRN-60178", "February 1, 1984",  "AET-100178", "Aetna",              "42 Benefit St, Providence, RI 02903",  "(401) 555-0178"),
        ("60179", "a-195900.E-60179", "Eleana Sandboxtest",  "MRN-60179", "February 27, 2015", "AET-100179", "Aetna",              "321 Elm St, Seattle, WA 98101",        "(206) 555-0179"),
        ("60180", "a-195900.E-60180", "Frankie Sandboxtest", "MRN-60180", "June 15, 1972",     "UHC-200180", "United Healthcare",  "100 Main St, Denver, CO 80202",        "(303) 555-0180"),
        ("60181", "a-195900.E-60181", "Anna Testpt",         "MRN-60181", "December 17, 1995", "CIG-300181", "Cigna",              "55 Water St, New York, NY 10004",      "(212) 555-0181"),
        ("60182", "a-195900.E-60182", "Rebecca Sandbox-Test","MRN-60182", "March 10, 1990",    "AET-100182", "Aetna",              "789 Pine Rd, Austin, TX 78701",        "(512) 555-0182"),
        ("60183", "a-195900.E-60183", "Gary Sandboxtest",    "MRN-60183", "April 18, 1948",    "AET-100183", "Aetna",              "456 Oak Ave, Chicago, IL 60601",       "(312) 555-0183"),
        ("60184", "a-195900.E-60184", "Dorrie Sandboxtest",  "MRN-60184", "November 23, 1949", "UHC-200184", "United Healthcare",  "100 Federal St, Boston, MA 02110",     "(617) 555-0184"),
    ];

    // ── Procedure fixtures ──────────────────────────────────────────────────
    private static readonly (string Code, string Name, string[] Diagnoses, string[] DiagnosisCodes)[] Procedures =
    [
        ("72148", "MRI Lumbar Spine w/o Contrast",    ["Low back pain",       "Lumbago with sciatica"],  ["M54.5",  "M54.41"]),
        ("72141", "MRI Cervical Spine w/o Contrast",  ["Cervical disc disorder", "Cervicalgia"],          ["M50.12", "M54.2"]),
        ("74177", "CT Abdomen/Pelvis w/ Contrast",    ["Gallstone",           "Abdominal pain"],          ["K80.20", "R10.9"]),
    ];

    // ── Provider fixtures ───────────────────────────────────────────────────
    private static readonly (string Id, string Name, string Npi)[] Providers =
    [
        ("DR001", "Dr. Kelli Smith",     "1234567890"),
        ("DR002", "Dr. Robert Kim",      "0987654321"),
        ("DR003", "Dr. Lisa Thompson",   "1122334455"),
        ("DR004", "Dr. Sarah Mitchell",  "5566778899"),
    ];

    // ── Clinical summary templates (by procedure) ───────────────────────────
    private static readonly string[][] ClinicalSummaries =
    [
        // 72148 — MRI Lumbar
        [
            "Patient presents with chronic low back pain radiating to left lower extremity for 6 weeks. Conservative treatment with NSAIDs and physical therapy for 4 weeks without improvement. Straight-leg raise positive at 40 degrees.",
            "Acute onset low back pain with progressive bilateral lower extremity numbness. History of lumbar disc herniation. MRI indicated to evaluate for recurrent disc pathology.",
            "Patient with worsening low back pain and radiculopathy. Failed 6 weeks of conservative management including physical therapy, NSAIDs, and epidural steroid injection. Neurological exam notable for diminished ankle reflex.",
            "Low back pain with sciatica unresponsive to 8 weeks of conservative care. Progressive motor weakness noted in L5 distribution. Urgent imaging indicated.",
        ],
        // 72141 — MRI Cervical
        [
            "Patient with neck pain and right upper extremity radiculopathy for 4 weeks. Failed conservative treatment with NSAIDs and cervical collar. Weakness noted in C6 distribution.",
            "Chronic cervicalgia with myelopathic symptoms including gait instability and hand clumsiness. MRI cervical spine indicated to evaluate for spinal cord compression.",
            "Neck pain with progressive right arm weakness and paresthesias. EMG suggests C5-C6 radiculopathy. Advanced imaging warranted.",
        ],
        // 74177 — CT Abdomen/Pelvis
        [
            "Right upper quadrant pain with nausea and elevated liver enzymes. Ultrasound showed cholelithiasis. CT indicated for surgical planning and evaluation of biliary anatomy.",
            "Diffuse abdominal pain with intermittent episodes for 3 months. Initial workup including labs and ultrasound inconclusive. CT with contrast for further evaluation.",
            "Patient with known gallstones presenting with acute right upper quadrant pain, fever, and leukocytosis. CT requested to evaluate for complications including cholecystitis or biliary obstruction.",
        ],
    ];

    // ── Criteria templates ──────────────────────────────────────────────────
    private static readonly CriterionModel[][] CriteriaTemplates =
    [
        // Template 0: All MET (for approved/submitted)
        [
            new() { Met = true, Label = "Medical necessity documented", Reason = "Clinical documentation supports medical necessity for the requested procedure" },
            new() { Met = true, Label = "Valid diagnosis code present", Reason = "ICD-10 code is an established indication for this procedure" },
            new() { Met = true, Label = "Conservative therapy attempted", Reason = "Patient has completed appropriate conservative management without adequate improvement" },
            new() { Met = true, Label = "Clinical rationale documented", Reason = "Ordering provider has documented clear clinical rationale with supporting exam findings" },
        ],
        // Template 1: Mostly MET, one unclear (for ready/waiting)
        [
            new() { Met = true, Label = "Medical necessity documented", Reason = "Clinical documentation supports medical necessity" },
            new() { Met = true, Label = "Valid diagnosis code present", Reason = "Diagnosis code supports the requested imaging study" },
            new() { Met = null, Label = "Conservative therapy attempted", Reason = "Documentation of prior conservative treatment is incomplete or unclear" },
            new() { Met = true, Label = "No duplicate imaging in 12 months", Reason = "No prior imaging of same anatomical region found" },
        ],
        // Template 2: Mixed with NOT_MET (for denied)
        [
            new() { Met = true, Label = "Medical necessity documented", Reason = "Basic clinical need is documented" },
            new() { Met = false, Label = "Conservative therapy attempted", Reason = "No documentation of conservative treatment trial of at least 4 weeks" },
            new() { Met = false, Label = "Clinical rationale documented", Reason = "Insufficient clinical rationale — no neurological exam findings or red flags documented" },
            new() { Met = true, Label = "Valid diagnosis code present", Reason = "Diagnosis code is valid but does not fully support the imaging request" },
            new() { Met = false, Label = "Prior imaging reviewed", Reason = "Recent imaging of same region exists but was not referenced in the order" },
        ],
        // Template 3: 5 criteria, all MET (for high-confidence approved)
        [
            new() { Met = true, Label = "Medical necessity documented", Reason = "Strong clinical indication with supporting documentation" },
            new() { Met = true, Label = "Valid diagnosis code present", Reason = "ICD-10 code is a primary indication per LCD guidelines" },
            new() { Met = true, Label = "Conservative therapy exhausted", Reason = "Completed full course of conservative management over recommended duration" },
            new() { Met = true, Label = "Red flag screening completed", Reason = "Neurological exam performed with documented findings supporting imaging" },
            new() { Met = true, Label = "No duplicate imaging", Reason = "No prior imaging of same anatomical region in the past 12 months" },
        ],
        // Template 4: 3 criteria, all MET (for submitted)
        [
            new() { Met = true, Label = "Medical necessity documented", Reason = "Procedure is medically necessary based on clinical presentation" },
            new() { Met = true, Label = "Valid diagnosis code present", Reason = "Diagnosis code is appropriate for the requested procedure" },
            new() { Met = true, Label = "Supporting documentation complete", Reason = "All required supporting documentation has been provided" },
        ],
    ];

    // ── Status distribution (total = 48) ────────────────────────────────────
    // processing: 6, ready: 8, submitted: 15, waiting_for_insurance: 8, approved: 9, denied: 2
    private static readonly (string Status, int Count)[] StatusDistribution =
    [
        ("processing",             6),
        ("ready",                  8),
        ("submitted",             15),
        ("waiting_for_insurance",  8),
        ("approved",               9),
        ("denied",                 2),
    ];

    /// <summary>
    /// Generates 48 deterministic demo <see cref="PARequestModel"/> instances.
    /// Uses a fixed random seed for reproducibility.
    /// </summary>
    /// <returns>A list of 48 PA request models with varied statuses, patients, and procedures.</returns>
    public static List<PARequestModel> GenerateSeedRequests()
    {
        var rng = new Random(42); // deterministic seed
        var now = DateTimeOffset.UtcNow;
        var results = new List<PARequestModel>(48);
        var index = 0;

        foreach (var (status, count) in StatusDistribution)
        {
            for (var i = 0; i < count; i++)
            {
                var patient = Patients[index % Patients.Length];
                var procedure = Procedures[index % Procedures.Length];
                var provider = Providers[index % Providers.Length];
                var diagnosisIdx = rng.Next(procedure.Diagnoses.Length);

                // Spread createdAt over the last 7 days
                var daysAgo = 7.0 * index / 47.0; // 0..7 evenly spread
                var createdAt = now.AddDays(-daysAgo).AddMinutes(-rng.Next(0, 60));

                // Confidence: 60-98 range
                var confidence = status == "denied"
                    ? 60 + rng.Next(0, 6) // 60-65 for denied
                    : 60 + rng.Next(0, 39); // 60-98 for others

                // Select criteria template based on status
                var criteriaTemplate = status switch
                {
                    "denied" => CriteriaTemplates[2],
                    "approved" => rng.Next(2) == 0 ? CriteriaTemplates[0] : CriteriaTemplates[3],
                    "processing" => CriteriaTemplates[4],
                    _ => rng.Next(3) == 0 ? CriteriaTemplates[1] : CriteriaTemplates[0],
                };

                // Build timestamp progression
                var readyAt = status is "processing"
                    ? (DateTimeOffset?)null
                    : createdAt.AddMinutes(rng.Next(5, 30));

                var submittedAt = status is "processing" or "ready"
                    ? (DateTimeOffset?)null
                    : readyAt!.Value.AddMinutes(rng.Next(2, 60));

                var reviewTimeSeconds = submittedAt.HasValue
                    ? rng.Next(30, 300)
                    : 0;

                // Clinical summary selection
                var summaryPool = ClinicalSummaries[index % Procedures.Length];
                var summary = summaryPool[index % summaryPool.Length];

                var serviceDate = now.AddDays(rng.Next(1, 30)).ToString("MMMM d, yyyy");

                results.Add(new PARequestModel
                {
                    Id = $"PA-SEED-{index + 1:D3}",
                    PatientId = patient.Id,
                    FhirPatientId = patient.FhirId,
                    Patient = new PatientModel
                    {
                        Id = patient.Id,
                        Name = patient.Name,
                        Mrn = patient.Mrn,
                        Dob = patient.Dob,
                        MemberId = patient.MemberId,
                        Payer = patient.Payer,
                        Address = patient.Address,
                        Phone = patient.Phone,
                    },
                    ProcedureCode = procedure.Code,
                    ProcedureName = procedure.Name,
                    Diagnosis = procedure.Diagnoses[diagnosisIdx],
                    DiagnosisCode = procedure.DiagnosisCodes[diagnosisIdx],
                    Payer = patient.Payer,
                    ProviderId = provider.Id,
                    Provider = provider.Name,
                    ProviderNpi = provider.Npi,
                    ServiceDate = serviceDate,
                    PlaceOfService = "Outpatient",
                    ClinicalSummary = summary,
                    Status = status,
                    Confidence = confidence,
                    Criteria = criteriaTemplate.Select(c => new CriterionModel
                    {
                        Label = c.Label,
                        Met = c.Met,
                        Reason = c.Reason,
                    }).ToList(),
                    CreatedAt = createdAt.ToString("o"),
                    UpdatedAt = (submittedAt ?? readyAt ?? createdAt).ToString("o"),
                    ReadyAt = readyAt?.ToString("o"),
                    SubmittedAt = submittedAt?.ToString("o"),
                    ReviewTimeSeconds = reviewTimeSeconds,
                });

                index++;
            }
        }

        return results;
    }
}
