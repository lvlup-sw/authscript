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
    payer: 'Blue Cross Blue Shield',
    address: '123 Main St',
    phone: '555-0100',
  },
  procedureCode: '72148',
  procedureName: 'MRI Lumbar Spine',
  diagnosis: 'Lumbar radiculopathy',
  diagnosisCode: 'M54.16',
  payer: 'Blue Cross Blue Shield',
  provider: 'Dr. Kelli Smith',
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

    const patientField = textFieldCalls.find((c) => c.field === 'Patient Name');
    expect(patientField?.value).toBe('Rebecca Sandbox');

    const providerField = textFieldCalls.find(
      (c) => c.field === 'Requesting Provider or Facility Name',
    );
    expect(providerField?.value).toBe('Dr. Kelli Smith');

    expect(checkBoxCalls).toContain('Review Type - Non-Urgent');
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
      (c) => c.field === 'Planned Service or Procedure Code Row 1',
    );
    expect(codeField?.value).toBe('72148');

    const memberField = textFieldCalls.find(
      (c) => c.field === 'Member or Medicaid ID Number',
    );
    expect(memberField?.value).toBe('ATH60182');

    const icdField = textFieldCalls.find(
      (c) => c.field === 'Diagnosis Description ICD Version Number',
    );
    expect(icdField?.value).toBe('10');
  });
});
