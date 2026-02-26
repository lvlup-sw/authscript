namespace Gateway.API.Tests.Services;

using Gateway.API.Services;

public class ReferenceDataServiceTests
{
    private ReferenceDataService CreateService() => new();

    // ── Procedures ───────────────────────────────────────────────────────

    [Test]
    public async Task Procedures_ContainsExpectedCount()
    {
        var svc = CreateService();
        await Assert.That(svc.Procedures.Count).IsGreaterThanOrEqualTo(9);
    }

    [Test]
    public async Task Procedures_AllHaveValidCodes()
    {
        var svc = CreateService();
        foreach (var p in svc.Procedures)
        {
            await Assert.That(string.IsNullOrWhiteSpace(p.Code)).IsFalse();
        }
    }

    // ── Medications ──────────────────────────────────────────────────────

    [Test]
    public async Task Medications_ContainsExpectedCount()
    {
        var svc = CreateService();
        await Assert.That(svc.Medications.Count).IsGreaterThanOrEqualTo(6);
    }

    // ── Diagnoses ────────────────────────────────────────────────────────

    [Test]
    public async Task Diagnoses_ContainsExpectedCount()
    {
        var svc = CreateService();
        await Assert.That(svc.Diagnoses.Count).IsGreaterThanOrEqualTo(10);
    }

    [Test]
    public async Task Diagnoses_AllHaveIcd10Codes()
    {
        var svc = CreateService();
        foreach (var d in svc.Diagnoses)
        {
            // ICD-10 pattern: letter followed by digits, optionally a dot and more digits
            await Assert.That(System.Text.RegularExpressions.Regex.IsMatch(d.Code, @"^[A-Z]\d{2}(\.\d{1,4})?$")).IsTrue();
        }
    }

    // ── Payers ────────────────────────────────────────────────────────────

    [Test]
    public async Task Payers_ContainsExpectedCount()
    {
        var svc = CreateService();
        await Assert.That(svc.Payers.Count).IsGreaterThanOrEqualTo(5);
    }

    // ── Providers ─────────────────────────────────────────────────────────

    [Test]
    public async Task Providers_ContainsExpectedCount()
    {
        var svc = CreateService();
        await Assert.That(svc.Providers.Count).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task Providers_AllHaveNpi()
    {
        var svc = CreateService();
        foreach (var p in svc.Providers)
        {
            await Assert.That(string.IsNullOrWhiteSpace(p.Npi)).IsFalse();
        }
    }

    // ── FindProcedureByCode ──────────────────────────────────────────────

    [Test]
    public async Task FindProcedureByCode_ExistingCode_ReturnsMatch()
    {
        var svc = CreateService();
        var result = svc.FindProcedureByCode("72148");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("MRI Lumbar Spine w/o Contrast");
    }

    [Test]
    public async Task FindProcedureByCode_NonExistentCode_ReturnsNull()
    {
        var svc = CreateService();
        var result = svc.FindProcedureByCode("99999");

        await Assert.That(result).IsNull();
    }

    // ── FindProviderById ─────────────────────────────────────────────────

    [Test]
    public async Task FindProviderById_ExistingId_ReturnsMatch()
    {
        var svc = CreateService();
        var result = svc.FindProviderById("DR001");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Dr. Kelli Smith");
    }

    [Test]
    public async Task FindProviderById_NonExistentId_ReturnsNull()
    {
        var svc = CreateService();
        var result = svc.FindProviderById("DR999");

        await Assert.That(result).IsNull();
    }
}
