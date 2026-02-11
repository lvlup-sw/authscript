namespace Gateway.API.Tests.Services;

using Gateway.API.GraphQL.Models;
using Gateway.API.Services;

public class MockDataServiceTests
{
    private MockDataService CreateService() => new();

    // ── Constructor / Bootstrap ──────────────────────────────────────────

    [Test]
    public async Task Constructor_BootstrapsRequests()
    {
        var svc = CreateService();
        var requests = svc.GetPARequests();

        // 6 hand-crafted + 31 generated approved
        await Assert.That(requests.Count).IsGreaterThanOrEqualTo(37);
    }

    [Test]
    public async Task Constructor_BootstrapIds_AreUnique()
    {
        var svc = CreateService();
        var ids = svc.GetPARequests().Select(r => r.Id).ToList();

        await Assert.That(ids.Distinct().Count()).IsEqualTo(ids.Count);
    }

    // ── Reference Data ──────────────────────────────────────────────────

    [Test]
    public async Task Procedures_ReturnsNonEmpty()
    {
        var svc = CreateService();
        await Assert.That(svc.Procedures.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Medications_ReturnsNonEmpty()
    {
        var svc = CreateService();
        await Assert.That(svc.Medications.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Payers_ReturnsNonEmpty()
    {
        var svc = CreateService();
        await Assert.That(svc.Payers.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Providers_ReturnsNonEmpty()
    {
        var svc = CreateService();
        await Assert.That(svc.Providers.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Diagnoses_ReturnsNonEmpty()
    {
        var svc = CreateService();
        await Assert.That(svc.Diagnoses.Count).IsGreaterThan(0);
    }

    // ── GetPARequest ────────────────────────────────────────────────────

    [Test]
    public async Task GetPARequest_ExistingId_ReturnsRequest()
    {
        var svc = CreateService();
        var result = svc.GetPARequest("PA-001");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo("PA-001");
    }

    [Test]
    public async Task GetPARequest_NonExistentId_ReturnsNull()
    {
        var svc = CreateService();
        var result = svc.GetPARequest("PA-NONEXISTENT");

        await Assert.That(result).IsNull();
    }

    // ── GetPAStats ──────────────────────────────────────────────────────

    [Test]
    public async Task GetPAStats_ReturnsTotals()
    {
        var svc = CreateService();
        var stats = svc.GetPAStats();

        await Assert.That(stats.Total).IsGreaterThan(0);
        await Assert.That(stats.Ready).IsGreaterThanOrEqualTo(0);
        await Assert.That(stats.Submitted).IsGreaterThanOrEqualTo(0);
    }

    // ── GetActivity ─────────────────────────────────────────────────────

    [Test]
    public async Task GetActivity_ReturnsUpToFiveItems()
    {
        var svc = CreateService();
        var activity = svc.GetActivity();

        await Assert.That(activity.Count).IsGreaterThan(0);
        await Assert.That(activity.Count).IsLessThanOrEqualTo(5);
    }

    // ── CreatePARequest ─────────────────────────────────────────────────

    [Test]
    public async Task CreatePARequest_ValidProcedure_ReturnsNewRequest()
    {
        var svc = CreateService();
        var patient = new PatientModel
        {
            Id = "test-1", Name = "Test Patient", Mrn = "MRN-1",
            Dob = "01/01/1990", MemberId = "MEM-1", Payer = "Aetna",
            Address = "123 Test St", Phone = "(555) 000-0000",
        };

        var result = svc.CreatePARequest(patient, "72148", "M54.5", "Low Back Pain");

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Status).IsEqualTo("draft");
        await Assert.That(result.Patient.Name).IsEqualTo("Test Patient");
        await Assert.That(result.ProcedureCode).IsEqualTo("72148");
    }

    [Test]
    public async Task CreatePARequest_ValidMedication_ReturnsNewRequest()
    {
        var svc = CreateService();
        var patient = new PatientModel
        {
            Id = "test-2", Name = "Med Patient", Mrn = "MRN-2",
            Dob = "02/02/1985", MemberId = "MEM-2", Payer = "Cigna",
            Address = "456 Med Ave", Phone = "(555) 111-1111",
        };

        var result = svc.CreatePARequest(patient, "J1745", "M06.9", "Rheumatoid Arthritis");

        await Assert.That(result).IsNotNull();
        await Assert.That(result.ProcedureCode).IsEqualTo("J1745");
    }

    [Test]
    public void CreatePARequest_InvalidCode_Throws()
    {
        var svc = CreateService();
        var patient = new PatientModel
        {
            Id = "test-3", Name = "Bad Patient", Mrn = "MRN-3",
            Dob = "03/03/1980", MemberId = "MEM-3", Payer = "Test",
            Address = "789 Bad Ln", Phone = "(555) 222-2222",
        };

        Assert.Throws<ArgumentException>(() =>
            svc.CreatePARequest(patient, "INVALID", "X00.0", "Nothing"));
    }

    [Test]
    public async Task CreatePARequest_IncreasesTotalCount()
    {
        var svc = CreateService();
        var before = svc.GetPARequests().Count;
        var patient = new PatientModel
        {
            Id = "test-cnt", Name = "Count Patient", Mrn = "MRN-C",
            Dob = "04/04/1995", MemberId = "MEM-C", Payer = "Humana",
            Address = "321 Ct", Phone = "(555) 333-3333",
        };

        svc.CreatePARequest(patient, "72148", "M54.5", "Low Back Pain");

        await Assert.That(svc.GetPARequests().Count).IsEqualTo(before + 1);
    }

    // ── UpdatePARequest ─────────────────────────────────────────────────

    [Test]
    public async Task UpdatePARequest_ExistingId_UpdatesFields()
    {
        var svc = CreateService();
        var result = svc.UpdatePARequest("PA-001",
            diagnosis: "Updated Diagnosis",
            diagnosisCode: "Z99.9",
            serviceDate: null,
            placeOfService: "Inpatient",
            clinicalSummary: null,
            criteria: null);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Diagnosis).IsEqualTo("Updated Diagnosis");
        await Assert.That(result.DiagnosisCode).IsEqualTo("Z99.9");
        await Assert.That(result.PlaceOfService).IsEqualTo("Inpatient");
    }

    [Test]
    public async Task UpdatePARequest_WithCriteriaAndReason_PreservesReason()
    {
        var svc = CreateService();
        var newCriteria = new List<CriterionModel>
        {
            new() { Met = true, Label = "Test criterion", Reason = "Because it's a test" },
        };

        var result = svc.UpdatePARequest("PA-001",
            diagnosis: null, diagnosisCode: null, serviceDate: null,
            placeOfService: null, clinicalSummary: null,
            criteria: newCriteria);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Criteria.Count).IsEqualTo(1);
        await Assert.That(result.Criteria[0].Reason).IsEqualTo("Because it's a test");
    }

    [Test]
    public async Task UpdatePARequest_NonExistentId_ReturnsNull()
    {
        var svc = CreateService();
        var result = svc.UpdatePARequest("PA-MISSING",
            diagnosis: "X", diagnosisCode: null, serviceDate: null,
            placeOfService: null, clinicalSummary: null, criteria: null);

        await Assert.That(result).IsNull();
    }

    // ── ProcessPARequestAsync ───────────────────────────────────────────

    [Test]
    public async Task ProcessPARequestAsync_DraftRequest_BecomesReady()
    {
        var svc = CreateService();
        var patient = new PatientModel
        {
            Id = "proc-1", Name = "Process Patient", Mrn = "MRN-P",
            Dob = "05/05/1975", MemberId = "MEM-P", Payer = "Aetna",
            Address = "100 Process Rd", Phone = "(555) 444-4444",
        };
        var created = svc.CreatePARequest(patient, "72148", "M54.5", "Low Back Pain");

        var result = await svc.ProcessPARequestAsync(created.Id);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Status).IsEqualTo("ready");
        await Assert.That(result.Confidence).IsGreaterThan(0);
        await Assert.That(result.ReadyAt).IsNotNull();
    }

    [Test]
    public async Task ProcessPARequestAsync_NonExistentId_ReturnsNull()
    {
        var svc = CreateService();
        var result = await svc.ProcessPARequestAsync("PA-NOPE");

        await Assert.That(result).IsNull();
    }

    // ── SubmitPARequest ─────────────────────────────────────────────────

    [Test]
    public async Task SubmitPARequest_ExistingId_BecomesWaitingForInsurance()
    {
        var svc = CreateService();
        var result = svc.SubmitPARequest("PA-001", addReviewTimeSeconds: 45);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Status).IsEqualTo("waiting_for_insurance");
        await Assert.That(result.SubmittedAt).IsNotNull();
        await Assert.That(result.ReviewTimeSeconds).IsGreaterThanOrEqualTo(45);
    }

    [Test]
    public async Task SubmitPARequest_NonExistentId_ReturnsNull()
    {
        var svc = CreateService();
        var result = svc.SubmitPARequest("PA-NOPE");

        await Assert.That(result).IsNull();
    }

    // ── AddReviewTime ───────────────────────────────────────────────────

    [Test]
    public async Task AddReviewTime_ExistingId_IncrementsSeconds()
    {
        var svc = CreateService();
        var before = svc.GetPARequest("PA-001")!.ReviewTimeSeconds;

        var result = svc.AddReviewTime("PA-001", 60);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ReviewTimeSeconds).IsEqualTo(before + 60);
    }

    [Test]
    public async Task AddReviewTime_NonExistentId_ReturnsNull()
    {
        var svc = CreateService();
        var result = svc.AddReviewTime("PA-NOPE", 10);

        await Assert.That(result).IsNull();
    }

    // ── DeletePARequest ─────────────────────────────────────────────────

    [Test]
    public async Task DeletePARequest_ExistingId_RemovesAndReturnsTrue()
    {
        var svc = CreateService();
        var before = svc.GetPARequests().Count;

        var deleted = svc.DeletePARequest("PA-001");

        await Assert.That(deleted).IsTrue();
        await Assert.That(svc.GetPARequests().Count).IsEqualTo(before - 1);
        await Assert.That(svc.GetPARequest("PA-001")).IsNull();
    }

    [Test]
    public async Task DeletePARequest_NonExistentId_ReturnsFalse()
    {
        var svc = CreateService();
        var deleted = svc.DeletePARequest("PA-NOPE");

        await Assert.That(deleted).IsFalse();
    }

    // ── Criteria with Reason field ──────────────────────────────────────

    [Test]
    public async Task BootstrapRequests_HaveCriteriaWithReasons()
    {
        var svc = CreateService();
        var pa001 = svc.GetPARequest("PA-001")!;

        await Assert.That(pa001.Criteria.Count).IsGreaterThan(0);
        // The "72148" procedure criteria should all have reasons
        foreach (var c in pa001.Criteria)
        {
            await Assert.That(c.Label).IsNotNull();
            await Assert.That(c.Reason).IsNotNull();
        }
    }

    [Test]
    public async Task BootstrapRequest_PA003_HasUnmetCriteria()
    {
        var svc = CreateService();
        var pa003 = svc.GetPARequest("PA-003")!;

        var unmet = pa003.Criteria.Where(c => c.Met == false).ToList();
        await Assert.That(unmet.Count).IsGreaterThan(0);
        // Each unmet criterion should have a reason explaining why
        foreach (var c in unmet)
        {
            await Assert.That(c.Reason).IsNotNull();
            await Assert.That(c.Reason!.Length).IsGreaterThan(0);
        }
    }

    // ── Success Rate ────────────────────────────────────────────────────

    [Test]
    public async Task BootstrapRequests_SuccessRateIsAbove95Percent()
    {
        var svc = CreateService();
        var all = svc.GetPARequests();
        var approved = all.Count(r => r.Status == "approved");
        var denied = all.Count(r => r.Status == "denied");
        var total = approved + denied;

        var rate = (double)approved / total * 100;
        await Assert.That(rate).IsGreaterThanOrEqualTo(95);
    }
}
