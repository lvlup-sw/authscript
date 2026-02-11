/**
 * Athenahealth Preview Practice 195900 test patients
 * These patients stay in the frontend - do not send to backend
 * @see https://docs.athenahealth.com/docs/test-patients
 */

export interface Patient {
  id: string;
  /** Athena Patient ID (numeric) */
  patientId: string;
  /** FHIR R4 Logical ID */
  fhirId: string;
  name: string;
  mrn: string;
  dob: string;
  memberId: string;
  payer: string;
  address: string;
  phone: string;
}

export const ATHENA_TEST_PATIENTS: Patient[] = [
  {
    id: '60178',
    patientId: '60178',
    fhirId: 'a-195900.E-60178',
    name: 'Donna Sandbox',
    mrn: '60178',
    dob: '03/15/1968',
    memberId: 'ATH60178',
    payer: 'Blue Cross Blue Shield',
    address: '123 Oak Street, Seattle, WA 98101',
    phone: '(206) 555-0123',
  },
  {
    id: '60179',
    patientId: '60179',
    fhirId: 'a-195900.E-60179',
    name: 'Eleana Sandbox',
    mrn: '60179',
    dob: '07/22/1975',
    memberId: 'ATH60179',
    payer: 'Aetna',
    address: '456 Pine Avenue, Bellevue, WA 98004',
    phone: '(425) 555-0456',
  },
  {
    id: '60180',
    patientId: '60180',
    fhirId: 'a-195900.E-60180',
    name: 'Frankie Sandbox',
    mrn: '60180',
    dob: '11/08/1982',
    memberId: 'ATH60180',
    payer: 'United Healthcare',
    address: '789 Cedar Lane, Kirkland, WA 98033',
    phone: '(425) 555-0789',
  },
  {
    id: '60181',
    patientId: '60181',
    fhirId: 'a-195900.E-60181',
    name: 'Anna Sandbox',
    mrn: '60181',
    dob: '02/28/1955',
    memberId: 'ATH60181',
    payer: 'Cigna',
    address: '321 Maple Drive, Redmond, WA 98052',
    phone: '(425) 555-0321',
  },
  {
    id: '60182',
    patientId: '60182',
    fhirId: 'a-195900.E-60182',
    name: 'Rebecca Sandbox',
    mrn: '60182',
    dob: '09/14/1990',
    memberId: 'ATH60182',
    payer: 'Blue Cross Blue Shield',
    address: '654 Birch Road, Tacoma, WA 98402',
    phone: '(253) 555-0654',
  },
  {
    id: '60183',
    patientId: '60183',
    fhirId: 'a-195900.E-60183',
    name: 'Gary Sandbox',
    mrn: '60183',
    dob: '05/20/1978',
    memberId: 'ATH60183',
    payer: 'Aetna',
    address: '111 Elm Street, Seattle, WA 98102',
    phone: '(206) 555-0199',
  },
  {
    id: '60184',
    patientId: '60184',
    fhirId: 'a-195900.E-60184',
    name: 'Dorrie Sandbox',
    mrn: '60184',
    dob: '12/03/1985',
    memberId: 'ATH60184',
    payer: 'United Healthcare',
    address: '222 Spruce Ave, Bellevue, WA 98005',
    phone: '(425) 555-0888',
  },
];
