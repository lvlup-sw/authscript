// =============================================================================
// Mock Data Service - Backend source of truth for dashboard reference data
// Migrated from apps/dashboard/src/lib/mockData.ts
// =============================================================================

using Gateway.API.GraphQL.Models;

namespace Gateway.API.Services;

/// <summary>
/// In-memory mock data service for patients, procedures, medications, payers, providers,
/// diagnoses, and PA requests. Used by GraphQL resolvers.
/// </summary>
public sealed class MockDataService : IDataService
{
    private readonly List<PARequestModel> _paRequests = [];
    private readonly object _lock = new();
    private int _paRequestCounter;

    // Bootstrap patients for initial PA requests only (Athena Preview Practice 195900)
    private static readonly PatientModel[] BootstrapPatients =
    [
        new PatientModel { Id = "60178", Name = "Donna Sandbox", Mrn = "60178", Dob = "03/15/1968", MemberId = "ATH60178", Payer = "Blue Cross Blue Shield", Address = "123 Oak Street, Seattle, WA 98101", Phone = "(206) 555-0123" },
        new PatientModel { Id = "60179", Name = "Eleana Sandbox", Mrn = "60179", Dob = "07/22/1975", MemberId = "ATH60179", Payer = "Aetna", Address = "456 Pine Avenue, Bellevue, WA 98004", Phone = "(425) 555-0456" },
        new PatientModel { Id = "60180", Name = "Frankie Sandbox", Mrn = "60180", Dob = "11/08/1982", MemberId = "ATH60180", Payer = "United Healthcare", Address = "789 Cedar Lane, Kirkland, WA 98033", Phone = "(425) 555-0789" },
    ];

    public MockDataService()
    {
        var provider = Providers[0];
        var now = DateTime.UtcNow;
        // Pending Review (ready) - readyAt = createdAt + 2 min
        var r1 = CreatePAFromProcedure(BootstrapPatients[0], Procedures[0], Diagnoses[0], provider, "ready", 85, now.AddMinutes(-5), readyAt: now.AddMinutes(-3)) with { Id = "PA-001" };
        var r2 = CreatePAFromProcedure(BootstrapPatients[1], Procedures[3], Diagnoses[2], provider, "ready", 92, now.AddMinutes(-12), readyAt: now.AddMinutes(-10)) with { Id = "PA-002" };
        var r3 = CreatePAFromProcedure(BootstrapPatients[2], Procedures[1], Diagnoses[5], provider, "ready", 58, now.AddMinutes(-45), readyAt: now.AddMinutes(-43)) with { Id = "PA-003" };
        // Waiting for Insurance - SHORTEST: 1 min to ready + 30s review = ~90s total
        var r4 = CreatePAFromProcedure(BootstrapPatients[1], Procedures[4], Diagnoses[4], provider, "waiting_for_insurance", 88, now.AddHours(-2), readyAt: now.AddHours(-2).AddMinutes(1), submittedAt: now.AddHours(-2).AddMinutes(1).AddSeconds(30), reviewTimeSeconds: 30) with { Id = "PA-004" };
        // History - MEDIUM: 2 min to ready + 2 min review = ~4 min total
        var r5 = CreatePAFromProcedure(BootstrapPatients[2], Procedures[5], Diagnoses[6], provider, "approved", 94, now.AddHours(-24), readyAt: now.AddHours(-24).AddMinutes(2), submittedAt: now.AddHours(-24).AddMinutes(4), reviewTimeSeconds: 120) with { Id = "PA-005" };
        // History - LONGEST: 3 min to ready + 15 min review = ~18 min total
        var r6 = CreatePAFromProcedure(BootstrapPatients[0], Procedures[1], Diagnoses[0], provider, "denied", 72, now.AddHours(-48), readyAt: now.AddHours(-48).AddMinutes(3), submittedAt: now.AddHours(-48).AddMinutes(18), reviewTimeSeconds: 900) with { Id = "PA-006" };

        lock (_lock)
        {
            _paRequests.AddRange([r1, r2, r3, r4, r5, r6]);
            _paRequestCounter = 7;
        }
    }

    public IReadOnlyList<ProcedureModel> Procedures { get; } =
    [
        new ProcedureModel { Code = "72148", Name = "MRI Lumbar Spine w/o Contrast", Category = "imaging", RequiresPA = true },
        new ProcedureModel { Code = "70553", Name = "MRI Brain w/ & w/o Contrast", Category = "imaging", RequiresPA = true },
        new ProcedureModel { Code = "72141", Name = "MRI Cervical Spine w/o Contrast", Category = "imaging", RequiresPA = true },
        new ProcedureModel { Code = "27447", Name = "Total Knee Replacement", Category = "surgery", RequiresPA = true },
        new ProcedureModel { Code = "27130", Name = "Total Hip Replacement", Category = "surgery", RequiresPA = true },
        new ProcedureModel { Code = "43239", Name = "Upper GI Endoscopy with Biopsy", Category = "surgery", RequiresPA = true },
        new ProcedureModel { Code = "29881", Name = "Knee Arthroscopy", Category = "surgery", RequiresPA = true },
        new ProcedureModel { Code = "97110", Name = "Physical Therapy - Therapeutic Exercises", Category = "therapy", RequiresPA = false },
        new ProcedureModel { Code = "90834", Name = "Psychotherapy, 45 minutes", Category = "therapy", RequiresPA = true },
    ];

    public IReadOnlyList<MedicationModel> Medications { get; } =
    [
        new MedicationModel { Code = "J1745", Name = "Infliximab (Remicade)", Dosage = "10mg/kg IV", Category = "Biologic", RequiresPA = true },
        new MedicationModel { Code = "J0129", Name = "Abatacept (Orencia)", Dosage = "125mg SC", Category = "Biologic", RequiresPA = true },
        new MedicationModel { Code = "J2357", Name = "Omalizumab (Xolair)", Dosage = "150mg SC", Category = "Biologic", RequiresPA = true },
        new MedicationModel { Code = "J9035", Name = "Bevacizumab (Avastin)", Dosage = "100mg IV", Category = "Oncology", RequiresPA = true },
        new MedicationModel { Code = "J1300", Name = "Eculizumab (Soliris)", Dosage = "300mg IV", Category = "Specialty", RequiresPA = true },
        new MedicationModel { Code = "J0585", Name = "Botulinum Toxin A", Dosage = "100 units", Category = "Neurology", RequiresPA = true },
    ];

    public IReadOnlyList<PayerModel> Payers { get; } =
    [
        new PayerModel { Id = "BCBS", Name = "Blue Cross Blue Shield", Phone = "1-800-262-2583", FaxNumber = "1-800-262-2584" },
        new PayerModel { Id = "AET", Name = "Aetna", Phone = "1-800-872-3862", FaxNumber = "1-800-872-3863" },
        new PayerModel { Id = "UHC", Name = "United Healthcare", Phone = "1-800-328-5979", FaxNumber = "1-800-328-5980" },
        new PayerModel { Id = "CIG", Name = "Cigna", Phone = "1-800-244-6224", FaxNumber = "1-800-244-6225" },
        new PayerModel { Id = "HUM", Name = "Humana", Phone = "1-800-457-4708", FaxNumber = "1-800-457-4709" },
    ];

    public IReadOnlyList<ProviderModel> Providers { get; } =
    [
        new ProviderModel { Id = "DR001", Name = "Dr. Amanda Martinez", Npi = "1234567890", Specialty = "Family Medicine" },
        new ProviderModel { Id = "DR002", Name = "Dr. Robert Kim", Npi = "0987654321", Specialty = "Orthopedic Surgery" },
        new ProviderModel { Id = "DR003", Name = "Dr. Lisa Thompson", Npi = "1122334455", Specialty = "Neurology" },
    ];

    public IReadOnlyList<DiagnosisModel> Diagnoses { get; } =
    [
        new DiagnosisModel { Code = "M54.5", Name = "Low Back Pain" },
        new DiagnosisModel { Code = "M54.2", Name = "Cervicalgia (Neck Pain)" },
        new DiagnosisModel { Code = "M17.11", Name = "Primary Osteoarthritis, Right Knee" },
        new DiagnosisModel { Code = "M17.12", Name = "Primary Osteoarthritis, Left Knee" },
        new DiagnosisModel { Code = "M16.11", Name = "Primary Osteoarthritis, Right Hip" },
        new DiagnosisModel { Code = "G43.909", Name = "Migraine, Unspecified" },
        new DiagnosisModel { Code = "K21.0", Name = "Gastroesophageal Reflux Disease with Esophagitis" },
        new DiagnosisModel { Code = "M06.9", Name = "Rheumatoid Arthritis, Unspecified" },
        new DiagnosisModel { Code = "L40.50", Name = "Psoriatic Arthritis" },
        new DiagnosisModel { Code = "J45.20", Name = "Mild Intermittent Asthma, Uncomplicated" },
    ];

    private static readonly Dictionary<string, CriterionModel[]> PolicyCriteria = new(StringComparer.OrdinalIgnoreCase)
    {
        ["72148"] = [new CriterionModel { Met = true, Label = "6+ weeks of conservative therapy documented" }, new CriterionModel { Met = true, Label = "Documentation of treatment failure or inadequate response" }, new CriterionModel { Met = null, Label = "Red flag neurological symptoms present (optional bypass)" }, new CriterionModel { Met = true, Label = "Valid ICD-10 diagnosis code" }],
        ["70553"] = [new CriterionModel { Met = true, Label = "Neurological symptoms documented" }, new CriterionModel { Met = true, Label = "Initial imaging (CT) performed" }, new CriterionModel { Met = true, Label = "Clinical indication for contrast study" }, new CriterionModel { Met = true, Label = "Valid ICD-10 diagnosis code" }],
        ["27447"] = [new CriterionModel { Met = true, Label = "Failed conservative treatment (6+ months)" }, new CriterionModel { Met = true, Label = "Radiographic evidence of severe arthritis" }, new CriterionModel { Met = true, Label = "Significant functional limitation documented" }, new CriterionModel { Met = null, Label = "BMI within acceptable range" }, new CriterionModel { Met = true, Label = "No active infections" }],
        ["default"] = [new CriterionModel { Met = true, Label = "Medical necessity documented" }, new CriterionModel { Met = true, Label = "Valid diagnosis code" }, new CriterionModel { Met = null, Label = "Prior authorization requirements met" }],
    };

    private static readonly Dictionary<string, string> ClinicalTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["72148"] = "Patient presents with chronic low back pain persisting for {duration} weeks. Conservative therapy including physical therapy ({pt_sessions} sessions) and NSAIDs ({nsaid}) has been attempted without adequate relief. MRI is requested to evaluate for possible disc herniation, spinal stenosis, or other structural abnormalities. {red_flags}",
        ["70553"] = "Patient presents with persistent headaches and neurological symptoms including {symptoms}. Initial workup including CT scan was inconclusive. MRI Brain with and without contrast is requested to rule out intracranial pathology, demyelinating disease, or other structural abnormalities.",
        ["27447"] = "Patient has end-stage osteoarthritis of the knee with significant functional limitation. Conservative management including physical therapy, NSAIDs, corticosteroid injections, and viscosupplementation has failed to provide adequate relief. X-rays demonstrate bone-on-bone arthritis. Total knee replacement is medically necessary.",
        ["27130"] = "Patient has end-stage osteoarthritis of the hip with severe pain and functional limitation. Conservative treatments have been exhausted. Imaging confirms advanced joint degeneration. Total hip replacement is recommended.",
        ["default"] = "Patient requires {procedure} for {diagnosis}. Clinical evaluation supports medical necessity for this procedure. Prior conservative treatments have been attempted as appropriate.",
    };

    public IReadOnlyList<PARequestModel> GetPARequests()
    {
        lock (_lock)
            return _paRequests.ToList();
    }

    public PARequestModel? GetPARequest(string id)
    {
        lock (_lock)
            return _paRequests.FirstOrDefault(r => r.Id == id);
    }

    public PAStatsModel GetPAStats()
    {
        const int lowConfidenceThreshold = 70;
        lock (_lock)
        {
            var ready = _paRequests.Count(r => r.Status == "ready");
            var submitted = _paRequests.Count(r => r.Status == "approved" || r.Status == "denied");
            var waitingForInsurance = _paRequests.Count(r => r.Status == "waiting_for_insurance");
            var attention = _paRequests.Count(r => r.Status == "ready" && r.Confidence < lowConfidenceThreshold);
            return new PAStatsModel { Ready = ready, Submitted = submitted, WaitingForInsurance = waitingForInsurance, Attention = attention, Total = _paRequests.Count };
        }
    }

    public IReadOnlyList<ActivityItemModel> GetActivity()
    {
        lock (_lock)
        {
            var activities = new List<ActivityItemModel>();
            var requests = _paRequests.OrderByDescending(r => r.UpdatedAt).Take(10).ToList();
            foreach (var req in requests)
            {
                var timeAgo = ToRelativeTimeAgo(req.UpdatedAt);
                if (req.Status == "approved" || req.Status == "denied" || req.Status == "waiting_for_insurance")
                    activities.Add(new ActivityItemModel { Id = $"act-{req.Id}-submitted", Action = "PA submitted", PatientName = req.Patient.Name, ProcedureCode = req.ProcedureCode, Time = timeAgo, Type = "success" });
                else if (req.Status == "ready")
                    activities.Add(new ActivityItemModel { Id = $"act-{req.Id}-ready", Action = "Ready for review", PatientName = req.Patient.Name, ProcedureCode = req.ProcedureCode, Time = timeAgo, Type = "ready" });
            }
            return activities.Take(5).ToList();
        }
    }

    private static string ToRelativeTimeAgo(string updatedAt)
    {
        if (string.IsNullOrEmpty(updatedAt)) return "—";
        if (!DateTimeOffset.TryParse(updatedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return "—";
        var ago = DateTimeOffset.UtcNow - dt;
        if (ago.TotalSeconds < 60) return $"{(int)ago.TotalSeconds}s ago";
        if (ago.TotalMinutes < 60) return $"{(int)ago.TotalMinutes}m ago";
        if (ago.TotalHours < 24) return $"{(int)ago.TotalHours}h ago";
        if (ago.TotalDays < 30) return $"{(int)ago.TotalDays}d ago";
        return $"{((int)ago.TotalDays / 30)}mo ago";
    }

    public PARequestModel CreatePARequest(PatientModel patient, string procedureCode, string diagnosisCode, string diagnosisName, string providerId = "DR001")
    {
        ProcedureModel? proc = Procedures.FirstOrDefault(p => p.Code == procedureCode);
        MedicationModel? med = Medications.FirstOrDefault(m => m.Code == procedureCode);
        var procedureName = proc?.Name ?? med?.Name ?? throw new ArgumentException($"Procedure/medication {procedureCode} not found");
        var diagnosis = new DiagnosisModel { Code = diagnosisCode, Name = diagnosisName };
        var provider = Providers.FirstOrDefault(p => p.Id == providerId) ?? Providers[0];
        var req = CreatePARequestInternal(patient, procedureCode, procedureName, diagnosis, provider, "draft", 0, DateTime.UtcNow);

        lock (_lock)
        {
            _paRequestCounter++;
            var id = $"PA-{_paRequestCounter:D3}";
            req = req with { Id = id };
            _paRequests.Insert(0, req);
        }
        return req;
    }

    public PARequestModel? UpdatePARequest(string id, string? diagnosis, string? diagnosisCode, string? serviceDate, string? placeOfService, string? clinicalSummary, IReadOnlyList<CriterionModel>? criteria)
    {
        lock (_lock)
        {
            var idx = _paRequests.FindIndex(r => r.Id == id);
            if (idx < 0) return null;

            var existing = _paRequests[idx];
            var updated = existing with
            {
                Diagnosis = diagnosis ?? existing.Diagnosis,
                DiagnosisCode = diagnosisCode ?? existing.DiagnosisCode,
                ServiceDate = serviceDate ?? existing.ServiceDate,
                PlaceOfService = placeOfService ?? existing.PlaceOfService,
                ClinicalSummary = clinicalSummary ?? existing.ClinicalSummary,
                Criteria = criteria ?? existing.Criteria,
                UpdatedAt = DateTime.UtcNow.ToString("O"),
            };
            _paRequests[idx] = updated;
            return updated;
        }
    }

    public async Task<PARequestModel?> ProcessPARequestAsync(string id, CancellationToken ct = default)
    {
        PARequestModel? updated = null;
        lock (_lock)
        {
            var idx = _paRequests.FindIndex(r => r.Id == id);
            if (idx < 0) return null;

        }

        await Task.Delay(2000, ct);

        lock (_lock)
        {
            var idx = _paRequests.FindIndex(r => r.Id == id);
            if (idx < 0) return null;

            var existing = _paRequests[idx];
            var confidence = Random.Shared.Next(70, 101);
            var criteria = existing.Criteria.Select(c => c with { Met = c.Met ?? (Random.Shared.NextDouble() > 0.3) }).ToList();
            var readyAt = DateTime.UtcNow.ToString("O");
            updated = existing with { Status = "ready", Confidence = confidence, Criteria = criteria, UpdatedAt = readyAt, ReadyAt = readyAt };
            _paRequests[idx] = updated;
        }
        return updated;
    }

    public PARequestModel? SubmitPARequest(string id, int addReviewTimeSeconds = 0)
    {
        lock (_lock)
        {
            var idx = _paRequests.FindIndex(r => r.Id == id);
            if (idx < 0) return null;

            var existing = _paRequests[idx];
            var submittedAt = DateTime.UtcNow.ToString("O");
            var totalReview = existing.ReviewTimeSeconds + addReviewTimeSeconds;
            var updated = existing with { Status = "waiting_for_insurance", UpdatedAt = submittedAt, SubmittedAt = submittedAt, ReviewTimeSeconds = totalReview };
            _paRequests[idx] = updated;
            return updated;
        }
    }

    public PARequestModel? AddReviewTime(string id, int seconds)
    {
        lock (_lock)
        {
            var idx = _paRequests.FindIndex(r => r.Id == id);
            if (idx < 0) return null;

            var existing = _paRequests[idx];
            var updated = existing with { ReviewTimeSeconds = existing.ReviewTimeSeconds + seconds };
            _paRequests[idx] = updated;
            return updated;
        }
    }

    public bool DeletePARequest(string id)
    {
        lock (_lock)
        {
            var removed = _paRequests.RemoveAll(r => r.Id == id);
            return removed > 0;
        }
    }

    /// <inheritdoc />
    public PARequestModel? ApprovePA(string id)
    {
        lock (_lock)
        {
            var index = _paRequests.FindIndex(p => p.Id == id);
            if (index < 0) return null;

            var existing = _paRequests[index];
            if (existing.Status != "waiting_for_insurance") return null;

            var updated = existing with { Status = "approved", UpdatedAt = DateTime.UtcNow.ToString("O") };
            _paRequests[index] = updated;
            return updated;
        }
    }

    /// <inheritdoc />
    public PARequestModel? DenyPA(string id, string reason)
    {
        lock (_lock)
        {
            var index = _paRequests.FindIndex(p => p.Id == id);
            if (index < 0) return null;

            var existing = _paRequests[index];
            if (existing.Status != "waiting_for_insurance") return null;

            var updated = existing with { Status = "denied", UpdatedAt = DateTime.UtcNow.ToString("O") };
            _paRequests[index] = updated;
            return updated;
        }
    }

    private static PARequestModel CreatePAFromProcedure(PatientModel patient, ProcedureModel procedure, DiagnosisModel diagnosis, ProviderModel provider, string status, int confidence, DateTime createdAt, DateTime? readyAt = null, DateTime? submittedAt = null, int reviewTimeSeconds = 0)
    {
        var baseReq = CreatePARequestInternal(patient, procedure.Code, procedure.Name, diagnosis, provider, status, confidence, createdAt);
        return baseReq with { ReadyAt = readyAt?.ToString("O"), SubmittedAt = submittedAt?.ToString("O"), ReviewTimeSeconds = reviewTimeSeconds };
    }

    private static PARequestModel CreatePARequestInternal(PatientModel patient, string procedureCode, string procedureName, DiagnosisModel diagnosis, ProviderModel provider, string status, int confidence, DateTime createdAt)
    {
        var criteria = PolicyCriteria.TryGetValue(procedureCode, out var c) ? c : PolicyCriteria["default"];
        var template = ClinicalTemplates.TryGetValue(procedureCode, out var t) ? t : ClinicalTemplates["default"];
        var clinicalSummary = template
            .Replace("{duration}", Random.Shared.Next(6, 12).ToString())
            .Replace("{pt_sessions}", Random.Shared.Next(4, 8).ToString())
            .Replace("{nsaid}", (new[] { "Ibuprofen 600mg TID", "Naproxen 500mg BID", "Meloxicam 15mg daily" })[Random.Shared.Next(3)])
            .Replace("{red_flags}", Random.Shared.NextDouble() > 0.7 ? "No red flag symptoms noted." : "Patient denies bowel/bladder dysfunction, saddle anesthesia, or progressive weakness.")
            .Replace("{symptoms}", "visual disturbances, dizziness, and cognitive changes")
            .Replace("{procedure}", procedureName)
            .Replace("{diagnosis}", diagnosis.Name);

        var metCount = criteria.Count(x => x.Met == true);
        var totalCount = criteria.Length;
        var rawConf = Math.Min(95, (int)((double)metCount / totalCount * 100) + Random.Shared.Next(10));
        var finalConfidence = confidence > 0 ? confidence : Math.Max(1, rawConf);

        var now = createdAt.ToString("O");
        return new PARequestModel
        {
            Id = "",
            PatientId = patient.Id,
            Patient = patient,
            ProcedureCode = procedureCode,
            ProcedureName = procedureName,
            Diagnosis = diagnosis.Name,
            DiagnosisCode = diagnosis.Code,
            Payer = patient.Payer,
            Provider = provider.Name,
            ProviderNpi = provider.Npi,
            ServiceDate = createdAt.ToString("MMMM d, yyyy"),
            PlaceOfService = "Outpatient",
            ClinicalSummary = clinicalSummary,
            Status = status,
            Confidence = finalConfidence,
            CreatedAt = now,
            UpdatedAt = now,
            ReadyAt = null,
            SubmittedAt = null,
            ReviewTimeSeconds = 0,
            Criteria = criteria.ToList(),
        };
    }
}
