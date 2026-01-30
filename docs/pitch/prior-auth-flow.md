The signal an app would need to know when and if to look for potential prior auth needs is called an ADT feed. It's a real-time, HL7-standardized electronic data stream from healthcare facilities (hospitals, labs) to external systems, alerting care teams instantly when a patient's location or status changes. ADT is Admission, Discharge, and Transfer which typically means that an "encounter" has ended which is the point where the provider is going to do their documentation. Encounter notes are usually unstructured free form text inputs where a lot of the documentation we will need would be. The encounter notes should have a "signing Provider."

In order to complete a prior authorization form I expect that an AI Agent would need to review the content in those notes for prior treatments and the rational for why the treatment is being requested in addition to some other relevant sections to collect other supporting evidence. These may also be preferred to as "progress notes."

Other very relevant areas of the patients record are going to be:

- Active Problem list (should be a list of DX codes indicating conditions the patient current has (or very recently has had)
- Medications
- Labs
- Diagnostic Reports
- Insurance/payer details (would be needed to do a check to see if a treatment requires prior authorization for a given patient
- Diagnosis codes (ICD-10) (some practice's may use SNOMED or IMO, but these should all have associated ICDs that they map to)
- CPT codes