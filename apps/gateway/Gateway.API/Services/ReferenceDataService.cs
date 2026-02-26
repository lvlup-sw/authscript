using Gateway.API.GraphQL.Models;

namespace Gateway.API.Services;

public sealed class ReferenceDataService
{
    public IReadOnlyList<ProcedureModel> Procedures { get; } =
    [
        new ProcedureModel { Code = "72148", Name = "MRI Lumbar Spine w/o Contrast", Category = "imaging", RequiresPA = true },
        new ProcedureModel { Code = "70553", Name = "MRI Brain w/ & w/o Contrast", Category = "imaging", RequiresPA = true },
        new ProcedureModel { Code = "72141", Name = "MRI Cervical Spine w/o Contrast", Category = "imaging", RequiresPA = true },
        new ProcedureModel { Code = "27447", Name = "Total Knee Replacement", Category = "surgery", RequiresPA = true },
        new ProcedureModel { Code = "27130", Name = "Total Hip Replacement", Category = "surgery", RequiresPA = true },
        new ProcedureModel { Code = "43239", Name = "Upper GI Endoscopy with Biopsy", Category = "surgery", RequiresPA = true },
        new ProcedureModel { Code = "29881", Name = "Knee Arthroscopy", Category = "surgery", RequiresPA = true },
        new ProcedureModel { Code = "93306", Name = "Echocardiogram, Complete", Category = "imaging", RequiresPA = true },
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
        new DiagnosisModel { Code = "J45.50", Name = "Severe Persistent Asthma, Uncomplicated" },
        new DiagnosisModel { Code = "I50.32", Name = "Chronic Diastolic Heart Failure" },
        new DiagnosisModel { Code = "I48.91", Name = "Unspecified Atrial Fibrillation" },
        new DiagnosisModel { Code = "F33.1", Name = "Major Depressive Disorder, Recurrent, Moderate" },
    ];

    public IReadOnlyList<PayerModel> Payers { get; } =
    [
        new PayerModel { Id = "BCBS", Name = "Blue Cross Blue Shield", Phone = "1-800-262-2583", FaxNumber = "1-800-262-2584" },
        new PayerModel { Id = "AET", Name = "Aetna", Phone = "1-800-872-3862", FaxNumber = "1-800-872-3863" },
        new PayerModel { Id = "UHC", Name = "United Healthcare", Phone = "1-800-328-5979", FaxNumber = "1-800-328-5980" },
        new PayerModel { Id = "CIG", Name = "Cigna", Phone = "1-800-244-6224", FaxNumber = "1-800-244-6225" },
        new PayerModel { Id = "HUM", Name = "Humana", Phone = "1-800-457-4708", FaxNumber = "1-800-457-4709" },
        new PayerModel { Id = "MCD", Name = "Medicare", Phone = "1-800-633-4227", FaxNumber = "1-800-633-4228" },
    ];

    public IReadOnlyList<ProviderModel> Providers { get; } =
    [
        new ProviderModel { Id = "DR001", Name = "Dr. Kelli Smith", Npi = "1234567890", Specialty = "Family Medicine" },
        new ProviderModel { Id = "DR002", Name = "Dr. Robert Kim", Npi = "0987654321", Specialty = "Orthopedic Surgery" },
        new ProviderModel { Id = "DR003", Name = "Dr. Lisa Thompson", Npi = "1122334455", Specialty = "Neurology" },
        new ProviderModel { Id = "DR004", Name = "Dr. Sarah Mitchell", Npi = "5566778899", Specialty = "Psychiatry" },
    ];

    public ProcedureModel? FindProcedureByCode(string code) =>
        Procedures.FirstOrDefault(p => p.Code == code);

    public MedicationModel? FindMedicationByCode(string code) =>
        Medications.FirstOrDefault(m => m.Code == code);

    public ProviderModel? FindProviderById(string id) =>
        Providers.FirstOrDefault(p => p.Id == id);
}
