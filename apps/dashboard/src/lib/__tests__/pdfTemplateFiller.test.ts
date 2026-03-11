import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { PARequest } from '@/api/graphqlService';

// Track calls via closures that survive hoisting
const textFieldCalls: Array<{ field: string; value: string }> = [];
const checkBoxCalls: string[] = [];
let flattenCalled = false;

vi.mock('pdf-lib', () => {
  return {
    PDFDocument: {
      load: vi.fn().mockResolvedValue({
        getForm: () => ({
          getTextField: (name: string) => ({
            setText: (value: string) => {
              textFieldCalls.push({ field: name, value });
            },
          }),
          getCheckBox: (name: string) => ({
            check: () => {
              checkBoxCalls.push(name);
            },
          }),
          flatten: () => {
            flattenCalled = true;
          },
        }),
        save: vi.fn().mockResolvedValue(new Uint8Array([1, 2, 3])),
      }),
    },
  };
});

const mockFetch = vi.fn();
global.fetch = mockFetch;

// Import after mocks
const { generateFilledPAForm } = await import('../pdfTemplateFiller');

const MOCK_REQUEST: PARequest = {
  id: 'PA-001',
  patientId: '60182',
  fhirPatientId: null,
  patient: {
    id: '60182',
    name: 'Rebecca Sandbox',
    mrn: '60182',
    dob: '09/14/1990',
    memberId: 'ATH60182',
    payer: 'Aetna',
    address: '123 Main St',
    phone: '555-0100',
  },
  procedureCode: '72148',
  procedureName: 'MRI Lumbar Spine',
  diagnosis: 'Lumbar radiculopathy',
  diagnosisCode: 'M54.16',
  payer: 'Aetna',
  provider: 'Kelli Smith, NP',
  providerNpi: '1234567890',
  serviceDate: '2026-03-01',
  placeOfService: '11',
  clinicalSummary: 'Patient summary',
  status: 'ready',
  confidence: 88,
  createdAt: '2026-02-25T00:00:00Z',
  updatedAt: '2026-02-25T00:00:00Z',
  readyAt: '2026-02-25T00:00:00Z',
  submittedAt: null,
  reviewTimeSeconds: 0,
  criteria: [],
};

describe('pdfTemplateFiller', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    textFieldCalls.length = 0;
    checkBoxCalls.length = 0;
    flattenCalled = false;
    mockFetch.mockResolvedValue({
      ok: true,
      arrayBuffer: () => Promise.resolve(new ArrayBuffer(10)),
      blob: () => Promise.resolve(new Blob(['fallback'])),
    });
  });

  it('generateFilledPAForm_Success_FillsTemplateFields', async () => {
    const blob = await generateFilledPAForm(MOCK_REQUEST);

    expect(blob).toBeInstanceOf(Blob);

    const patientField = textFieldCalls.find((c) => c.field === 'Patient Name First Last');
    expect(patientField?.value).toBe('Rebecca Sandbox');

    const providerField = textFieldCalls.find(
      (c) => c.field === 'Physician Name First Last',
    );
    expect(providerField?.value).toBe('Kelli Smith, NP');

    expect(checkBoxCalls).toContain('MRI');
    expect(checkBoxCalls).toContain('SPINE');
    expect(checkBoxCalls).toContain('Radiculopathy');
    expect(flattenCalled).toBe(true);
  });

  it('generateFilledPAForm_FetchFails_ReturnsFallback', async () => {
    mockFetch
      .mockResolvedValueOnce({ ok: false, status: 404 })
      .mockResolvedValueOnce({
        ok: true,
        blob: () => Promise.resolve(new Blob(['fallback-pdf'])),
      });

    const blob = await generateFilledPAForm(MOCK_REQUEST);

    expect(blob).toBeInstanceOf(Blob);
    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  it('generateFilledPAForm_MapsRequiredFields', async () => {
    await generateFilledPAForm(MOCK_REQUEST);

    const codeField = textFieldCalls.find(
      (c) => c.field === 'CPT Codes',
    );
    expect(codeField?.value).toBe('72148');

    const memberField = textFieldCalls.find(
      (c) => c.field === 'Member ID',
    );
    expect(memberField?.value).toBe('ATH60182');

    const diagnosisField = textFieldCalls.find(
      (c) => c.field === 'ICD Diagnosis Codes',
    );
    expect(diagnosisField?.value).toBe('M54.16');

    const healthPlanField = textFieldCalls.find(
      (c) => c.field === 'Health Plan',
    );
    expect(healthPlanField?.value).toBe('Aetna');

    const providerField2 = textFieldCalls.find(
      (c) => c.field === 'Physician Name First Last',
    );
    expect(providerField2?.value).toBe('Kelli Smith, NP');
  });
});
