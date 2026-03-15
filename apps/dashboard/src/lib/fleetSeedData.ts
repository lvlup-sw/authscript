/**
 * Deterministic seed data for the Fleet Dashboard demo.
 * Generates 48 PA requests distributed across 6 statuses using
 * the 7 Athena test patients, 3 CPT codes, and 3 payers.
 */

import { ATHENA_TEST_PATIENTS } from './patients';
import type { PARequest, Patient, Criterion } from '@/api/graphqlService';

/** Extended status set for fleet demo (superset of backend PARequest.status) */
export type FleetStatus =
  | 'processing'
  | 'ready'
  | 'submitted'
  | 'waiting_for_insurance'
  | 'approved'
  | 'denied';

/** PARequest with fleet-specific status */
export type FleetPARequest = Omit<PARequest, 'status'> & {
  status: FleetStatus;
};

const PROCEDURES = [
  { code: '72148', name: 'MRI Lumbar Spine without Contrast' },
  { code: '72141', name: 'MRI Cervical Spine without Contrast' },
  { code: '74177', name: 'CT Abdomen and Pelvis with Contrast' },
] as const;

const PAYERS = ['Aetna', 'United Healthcare', 'Cigna'] as const;

const DIAGNOSES = [
  { code: 'M54.5', name: 'Low back pain' },
  { code: 'M54.2', name: 'Cervicalgia' },
  { code: 'R10.9', name: 'Unspecified abdominal pain' },
] as const;

/**
 * Status distribution: 48 total
 * processing=6, ready=8, submitted=15, waiting_for_insurance=8, approved=9, denied=2
 */
const STATUS_DISTRIBUTION: FleetStatus[] = [
  // 6 processing
  ...Array<FleetStatus>(6).fill('processing'),
  // 8 ready
  ...Array<FleetStatus>(8).fill('ready'),
  // 15 submitted
  ...Array<FleetStatus>(15).fill('submitted'),
  // 8 waiting_for_insurance
  ...Array<FleetStatus>(8).fill('waiting_for_insurance'),
  // 9 approved
  ...Array<FleetStatus>(9).fill('approved'),
  // 2 denied
  ...Array<FleetStatus>(2).fill('denied'),
];

function makeCriteria(index: number): Criterion[] {
  const met = index % 3 !== 2;
  return [
    { met: true, label: 'Valid ICD-10 diagnosis', reason: 'Diagnosis code is covered' },
    { met, label: 'Prior conservative treatment', reason: met ? 'Documented' : 'Not documented' },
    { met: true, label: 'Clinical documentation', reason: 'Encounter note available' },
  ];
}

/** Deterministic confidence: 60-98 based on index */
function computeConfidence(index: number): number {
  return 60 + (index * 7) % 39; // yields values 60..98 deterministically
}

/** Base date for timestamp generation */
const BASE_DATE = new Date('2026-03-10T08:00:00Z');

function makeTimestamp(index: number, offsetHours: number): string {
  const d = new Date(BASE_DATE.getTime() + ((index * 3 + offsetHours) % 168) * 3600000);
  return d.toISOString();
}

export function generateFleetData(): FleetPARequest[] {
  const patients = ATHENA_TEST_PATIENTS;

  return STATUS_DISTRIBUTION.map((status, i): FleetPARequest => {
    const patient = patients[i % patients.length];
    const procedure = PROCEDURES[i % PROCEDURES.length];
    const payer = PAYERS[i % PAYERS.length];
    const diagnosis = DIAGNOSES[i % DIAGNOSES.length];
    const confidence = computeConfidence(i);

    const createdAt = makeTimestamp(i, 0);
    const updatedAt = makeTimestamp(i, 2);

    const patientModel: Patient = {
      id: patient.id,
      name: patient.name,
      mrn: patient.mrn,
      dob: patient.dob,
      memberId: patient.memberId,
      payer: patient.payer,
      address: patient.address,
      phone: patient.phone,
    };

    return {
      id: `fleet-${String(i + 1).padStart(3, '0')}`,
      patientId: patient.patientId,
      fhirPatientId: patient.fhirId,
      patient: patientModel,
      procedureCode: procedure.code,
      procedureName: procedure.name,
      diagnosis: diagnosis.name,
      diagnosisCode: diagnosis.code,
      payer: payer,
      provider: 'Dr. Sarah Chen',
      providerNpi: '1234567890',
      serviceDate: '2026-03-15',
      placeOfService: 'Office',
      clinicalSummary: `Patient presents for ${procedure.name}`,
      status,
      confidence,
      createdAt,
      updatedAt,
      readyAt: ['ready', 'submitted', 'waiting_for_insurance', 'approved', 'denied'].includes(status)
        ? makeTimestamp(i, 4)
        : null,
      submittedAt: ['submitted', 'waiting_for_insurance', 'approved', 'denied'].includes(status)
        ? makeTimestamp(i, 6)
        : null,
      criteria: makeCriteria(i),
    };
  });
}
