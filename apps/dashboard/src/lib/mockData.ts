// Mock data for the AuthScript dashboard

export interface Patient {
  id: string;
  name: string;
  mrn: string;
  dob: string;
  memberId: string;
  payer: string;
  address: string;
  phone: string;
}

export interface Procedure {
  code: string;
  name: string;
  category: 'imaging' | 'surgery' | 'therapy' | 'medication' | 'lab';
  requiresPA: boolean;
}

export interface Medication {
  code: string;
  name: string;
  dosage: string;
  category: string;
  requiresPA: boolean;
}

export interface Payer {
  id: string;
  name: string;
  phone: string;
  faxNumber: string;
}

export interface Provider {
  id: string;
  name: string;
  npi: string;
  specialty: string;
}

export interface PARequest {
  id: string;
  patientId: string;
  patient: Patient;
  procedureCode: string;
  procedureName: string;
  diagnosis: string;
  diagnosisCode: string;
  payer: string;
  provider: string;
  providerNpi: string;
  serviceDate: string;
  placeOfService: string;
  clinicalSummary: string;
  status: 'draft' | 'processing' | 'ready' | 'submitted' | 'approved' | 'denied';
  confidence: number;
  createdAt: string;
  updatedAt: string;
  criteria: { met: boolean | null; label: string }[];
}

// Mock Patients
export const PATIENTS: Patient[] = [
  {
    id: 'P001',
    name: 'Sarah Johnson',
    mrn: '10045892',
    dob: '03/15/1968',
    memberId: 'XYZ123456789',
    payer: 'Blue Cross Blue Shield',
    address: '123 Oak Street, Seattle, WA 98101',
    phone: '(206) 555-0123',
  },
  {
    id: 'P002',
    name: 'Michael Chen',
    mrn: '10078234',
    dob: '07/22/1975',
    memberId: 'AET987654321',
    payer: 'Aetna',
    address: '456 Pine Avenue, Bellevue, WA 98004',
    phone: '(425) 555-0456',
  },
  {
    id: 'P003',
    name: 'Emily Rodriguez',
    mrn: '10056781',
    dob: '11/08/1982',
    memberId: 'UHC456789123',
    payer: 'United Healthcare',
    address: '789 Cedar Lane, Kirkland, WA 98033',
    phone: '(425) 555-0789',
  },
  {
    id: 'P004',
    name: 'James Wilson',
    mrn: '10089012',
    dob: '02/28/1955',
    memberId: 'CIG321654987',
    payer: 'Cigna',
    address: '321 Maple Drive, Redmond, WA 98052',
    phone: '(425) 555-0321',
  },
  {
    id: 'P005',
    name: 'Maria Garcia',
    mrn: '10034567',
    dob: '09/14/1990',
    memberId: 'BCBS789456123',
    payer: 'Blue Cross Blue Shield',
    address: '654 Birch Road, Tacoma, WA 98402',
    phone: '(253) 555-0654',
  },
];

// Mock Procedures
export const PROCEDURES: Procedure[] = [
  { code: '72148', name: 'MRI Lumbar Spine w/o Contrast', category: 'imaging', requiresPA: true },
  { code: '70553', name: 'MRI Brain w/ & w/o Contrast', category: 'imaging', requiresPA: true },
  { code: '72141', name: 'MRI Cervical Spine w/o Contrast', category: 'imaging', requiresPA: true },
  { code: '27447', name: 'Total Knee Replacement', category: 'surgery', requiresPA: true },
  { code: '27130', name: 'Total Hip Replacement', category: 'surgery', requiresPA: true },
  { code: '43239', name: 'Upper GI Endoscopy with Biopsy', category: 'surgery', requiresPA: true },
  { code: '29881', name: 'Knee Arthroscopy', category: 'surgery', requiresPA: true },
  { code: '97110', name: 'Physical Therapy - Therapeutic Exercises', category: 'therapy', requiresPA: false },
  { code: '90834', name: 'Psychotherapy, 45 minutes', category: 'therapy', requiresPA: true },
];

// Mock Medications
export const MEDICATIONS: Medication[] = [
  { code: 'J1745', name: 'Infliximab (Remicade)', dosage: '10mg/kg IV', category: 'Biologic', requiresPA: true },
  { code: 'J0129', name: 'Abatacept (Orencia)', dosage: '125mg SC', category: 'Biologic', requiresPA: true },
  { code: 'J2357', name: 'Omalizumab (Xolair)', dosage: '150mg SC', category: 'Biologic', requiresPA: true },
  { code: 'J9035', name: 'Bevacizumab (Avastin)', dosage: '100mg IV', category: 'Oncology', requiresPA: true },
  { code: 'J1300', name: 'Eculizumab (Soliris)', dosage: '300mg IV', category: 'Specialty', requiresPA: true },
  { code: 'J0585', name: 'Botulinum Toxin A', dosage: '100 units', category: 'Neurology', requiresPA: true },
];

// Mock Payers
export const PAYERS: Payer[] = [
  { id: 'BCBS', name: 'Blue Cross Blue Shield', phone: '1-800-262-2583', faxNumber: '1-800-262-2584' },
  { id: 'AET', name: 'Aetna', phone: '1-800-872-3862', faxNumber: '1-800-872-3863' },
  { id: 'UHC', name: 'United Healthcare', phone: '1-800-328-5979', faxNumber: '1-800-328-5980' },
  { id: 'CIG', name: 'Cigna', phone: '1-800-244-6224', faxNumber: '1-800-244-6225' },
  { id: 'HUM', name: 'Humana', phone: '1-800-457-4708', faxNumber: '1-800-457-4709' },
];

// Mock Providers
export const PROVIDERS: Provider[] = [
  { id: 'DR001', name: 'Dr. Amanda Martinez', npi: '1234567890', specialty: 'Family Medicine' },
  { id: 'DR002', name: 'Dr. Robert Kim', npi: '0987654321', specialty: 'Orthopedic Surgery' },
  { id: 'DR003', name: 'Dr. Lisa Thompson', npi: '1122334455', specialty: 'Neurology' },
];

// Mock Diagnoses
export const DIAGNOSES = [
  { code: 'M54.5', name: 'Low Back Pain' },
  { code: 'M54.2', name: 'Cervicalgia (Neck Pain)' },
  { code: 'M17.11', name: 'Primary Osteoarthritis, Right Knee' },
  { code: 'M17.12', name: 'Primary Osteoarthritis, Left Knee' },
  { code: 'M16.11', name: 'Primary Osteoarthritis, Right Hip' },
  { code: 'G43.909', name: 'Migraine, Unspecified' },
  { code: 'K21.0', name: 'Gastroesophageal Reflux Disease with Esophagitis' },
  { code: 'M06.9', name: 'Rheumatoid Arthritis, Unspecified' },
  { code: 'L40.50', name: 'Psoriatic Arthritis' },
  { code: 'J45.20', name: 'Mild Intermittent Asthma, Uncomplicated' },
];

// Clinical Summary Templates
export const CLINICAL_TEMPLATES: Record<string, string> = {
  '72148': `Patient presents with chronic low back pain persisting for {duration} weeks. Conservative therapy including physical therapy ({pt_sessions} sessions) and NSAIDs ({nsaid}) has been attempted without adequate relief. MRI is requested to evaluate for possible disc herniation, spinal stenosis, or other structural abnormalities. {red_flags}`,
  '70553': `Patient presents with persistent headaches and neurological symptoms including {symptoms}. Initial workup including CT scan was inconclusive. MRI Brain with and without contrast is requested to rule out intracranial pathology, demyelinating disease, or other structural abnormalities.`,
  '27447': `Patient has end-stage osteoarthritis of the knee with significant functional limitation. Conservative management including physical therapy, NSAIDs, corticosteroid injections, and viscosupplementation has failed to provide adequate relief. X-rays demonstrate bone-on-bone arthritis. Total knee replacement is medically necessary.`,
  '27130': `Patient has end-stage osteoarthritis of the hip with severe pain and functional limitation. Conservative treatments have been exhausted. Imaging confirms advanced joint degeneration. Total hip replacement is recommended.`,
  'default': `Patient requires {procedure} for {diagnosis}. Clinical evaluation supports medical necessity for this procedure. Prior conservative treatments have been attempted as appropriate.`,
};

// Policy Criteria Templates
export const POLICY_CRITERIA: Record<string, { met: boolean | null; label: string }[]> = {
  '72148': [
    { met: true, label: '6+ weeks of conservative therapy documented' },
    { met: true, label: 'Documentation of treatment failure or inadequate response' },
    { met: null, label: 'Red flag neurological symptoms present (optional bypass)' },
    { met: true, label: 'Valid ICD-10 diagnosis code' },
  ],
  '70553': [
    { met: true, label: 'Neurological symptoms documented' },
    { met: true, label: 'Initial imaging (CT) performed' },
    { met: true, label: 'Clinical indication for contrast study' },
    { met: true, label: 'Valid ICD-10 diagnosis code' },
  ],
  '27447': [
    { met: true, label: 'Failed conservative treatment (6+ months)' },
    { met: true, label: 'Radiographic evidence of severe arthritis' },
    { met: true, label: 'Significant functional limitation documented' },
    { met: null, label: 'BMI within acceptable range' },
    { met: true, label: 'No active infections' },
  ],
  'default': [
    { met: true, label: 'Medical necessity documented' },
    { met: true, label: 'Valid diagnosis code' },
    { met: null, label: 'Prior authorization requirements met' },
  ],
};

// Generate AI mock response for PA
export function generateMockPAData(
  patient: Patient,
  procedure: Procedure | Medication,
  diagnosis: { code: string; name: string },
  provider: Provider
): Partial<PARequest> {
  const procedureCode = procedure.code;
  const template = CLINICAL_TEMPLATES[procedureCode] || CLINICAL_TEMPLATES['default'];
  
  // Generate clinical summary with some randomization
  const clinicalSummary = template
    .replace('{duration}', String(Math.floor(Math.random() * 6) + 6))
    .replace('{pt_sessions}', String(Math.floor(Math.random() * 4) + 4))
    .replace('{nsaid}', ['Ibuprofen 600mg TID', 'Naproxen 500mg BID', 'Meloxicam 15mg daily'][Math.floor(Math.random() * 3)])
    .replace('{red_flags}', Math.random() > 0.7 ? 'No red flag symptoms noted.' : 'Patient denies bowel/bladder dysfunction, saddle anesthesia, or progressive weakness.')
    .replace('{symptoms}', 'visual disturbances, dizziness, and cognitive changes')
    .replace('{procedure}', procedure.name)
    .replace('{diagnosis}', diagnosis.name);

  const criteria = POLICY_CRITERIA[procedureCode] || POLICY_CRITERIA['default'];
  const metCount = criteria.filter(c => c.met === true).length;
  const totalCount = criteria.length;
  const raw = Math.min(95, Math.floor((metCount / totalCount) * 100) + Math.floor(Math.random() * 10));
  const confidence = Math.max(1, raw); // Never show 0% AI confidence

  return {
    patientId: patient.id,
    patient,
    procedureCode: procedure.code,
    procedureName: procedure.name,
    diagnosis: diagnosis.name,
    diagnosisCode: diagnosis.code,
    payer: patient.payer,
    provider: provider.name,
    providerNpi: provider.npi,
    serviceDate: new Date().toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' }),
    placeOfService: 'Outpatient',
    clinicalSummary,
    confidence,
    criteria: criteria.map(c => ({ ...c })),
  };
}

// Activity types
export interface ActivityItem {
  id: string;
  action: string;
  patientName: string;
  procedureCode: string;
  time: string;
  type: 'success' | 'ready' | 'processing' | 'info';
}

// Generate mock activity
export function generateMockActivity(requests: PARequest[]): ActivityItem[] {
  const activities: ActivityItem[] = [];
  
  requests.forEach((req, index) => {
    if (req.status === 'submitted' || req.status === 'approved') {
      activities.push({
        id: `act-${req.id}-submitted`,
        action: 'PA submitted',
        patientName: req.patient.name,
        procedureCode: req.procedureCode,
        time: `${(index + 1) * 2}h ago`,
        type: 'success',
      });
    }
    if (req.status === 'ready') {
      activities.push({
        id: `act-${req.id}-ready`,
        action: 'Ready for review',
        patientName: req.patient.name,
        procedureCode: req.procedureCode,
        time: `${(index + 1) * 5}m ago`,
        type: 'ready',
      });
    }
    if (req.status === 'processing') {
      activities.push({
        id: `act-${req.id}-processing`,
        action: 'Processing started',
        patientName: req.patient.name,
        procedureCode: req.procedureCode,
        time: `${(index + 1) * 12}m ago`,
        type: 'processing',
      });
    }
  });

  return activities.slice(0, 5);
}
