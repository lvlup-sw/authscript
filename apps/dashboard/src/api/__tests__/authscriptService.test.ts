import { describe, it, expect, vi, beforeEach } from 'vitest';
import { authscriptService } from '../authscriptService';
import { customFetch } from '../customFetch';

// Mock customFetch
vi.mock('../customFetch', () => ({
  customFetch: vi.fn(),
  default: vi.fn(),
}));

const mockCustomFetch = vi.mocked(customFetch);

describe('authscriptService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('triggerAnalysis', () => {
    it('triggerAnalysis_ValidInput_CallsApiAndReturnsTransactionId', async () => {
      mockCustomFetch.mockResolvedValueOnce({ transactionId: 'txn-123' });

      const result = await authscriptService.triggerAnalysis({
        patientId: 'patient-001',
        procedureCode: '72148',
      });

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis',
        method: 'POST',
        body: JSON.stringify({
          patientId: 'patient-001',
          procedureCode: '72148',
        }),
      });
      expect(result.transactionId).toBe('txn-123');
    });

    it('triggerAnalysis_MissingPatientId_ThrowsError', async () => {
      await expect(
        authscriptService.triggerAnalysis({
          patientId: '',
          procedureCode: '72148',
        })
      ).rejects.toThrow('patientId is required');
    });

    it('triggerAnalysis_MissingProcedureCode_ThrowsError', async () => {
      await expect(
        authscriptService.triggerAnalysis({
          patientId: 'patient-001',
          procedureCode: '',
        })
      ).rejects.toThrow('procedureCode is required');
    });

    it('triggerAnalysis_WithEncounterId_IncludesInRequest', async () => {
      mockCustomFetch.mockResolvedValueOnce({ transactionId: 'txn-123' });

      await authscriptService.triggerAnalysis({
        patientId: 'patient-001',
        procedureCode: '72148',
        encounterId: 'enc-456',
      });

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis',
        method: 'POST',
        body: JSON.stringify({
          patientId: 'patient-001',
          procedureCode: '72148',
          encounterId: 'enc-456',
        }),
      });
    });

    it('triggerAnalysis_WhitespacePatientId_ThrowsError', async () => {
      await expect(
        authscriptService.triggerAnalysis({
          patientId: '   ',
          procedureCode: '72148',
        })
      ).rejects.toThrow('patientId is required');
    });

    it('triggerAnalysis_TrimsWhitespace_InRequestPayload', async () => {
      mockCustomFetch.mockResolvedValueOnce({ transactionId: 'txn-123' });

      await authscriptService.triggerAnalysis({
        patientId: '  patient-001  ',
        procedureCode: '  72148  ',
      });

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis',
        method: 'POST',
        body: JSON.stringify({
          patientId: 'patient-001',
          procedureCode: '72148',
        }),
      });
    });
  });

  describe('getAnalysisResult', () => {
    it('getAnalysisResult_ValidId_ReturnsAnalysis', async () => {
      const mockResponse = {
        transactionId: 'txn-123',
        patientName: 'John Doe',
        patientDob: '1985-03-15',
        memberId: 'MEM123',
        diagnosisCodes: ['M54.5'],
        procedureCode: '72148',
        clinicalSummary: 'Test summary',
        supportingEvidence: [],
        recommendation: 'APPROVE',
        confidenceScore: 0.92,
        fieldMappings: {},
      };
      mockCustomFetch.mockResolvedValueOnce(mockResponse);

      const result = await authscriptService.getAnalysisResult('txn-123');

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis/txn-123',
      });
      expect(result.patientName).toBe('John Doe');
    });

    it('getAnalysisResult_EmptyId_ThrowsError', async () => {
      await expect(
        authscriptService.getAnalysisResult('')
      ).rejects.toThrow('transactionId is required');
    });

    it('getAnalysisResult_UrlEncodesTransactionId', async () => {
      mockCustomFetch.mockResolvedValueOnce({
        transactionId: 'txn/123',
        patientName: 'Test',
        patientDob: '1985-01-01',
        memberId: 'M1',
        diagnosisCodes: [],
        procedureCode: '72148',
        clinicalSummary: 'Test',
        supportingEvidence: [],
        recommendation: 'APPROVE',
        confidenceScore: 0.9,
        fieldMappings: {},
      });

      await authscriptService.getAnalysisResult('txn/123');

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis/txn%2F123',
      });
    });
  });

  describe('getAnalysisStatus', () => {
    it('getAnalysisStatus_ValidId_ReturnsStatus', async () => {
      const mockStatus = {
        transactionId: 'txn-123',
        step: 'analyzing',
        message: 'Processing documents...',
        progress: 45,
        timestamp: '2026-01-24T10:30:00Z',
        status: 'in_progress',
      };
      mockCustomFetch.mockResolvedValueOnce(mockStatus);

      const result = await authscriptService.getAnalysisStatus('txn-123');

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis/txn-123/status',
      });
      expect(result.step).toBe('analyzing');
      expect(result.progress).toBe(45);
    });

    it('getAnalysisStatus_EmptyId_ThrowsError', async () => {
      await expect(
        authscriptService.getAnalysisStatus('')
      ).rejects.toThrow('transactionId is required');
    });

    it('getAnalysisStatus_UrlEncodesTransactionId', async () => {
      mockCustomFetch.mockResolvedValueOnce({
        transactionId: 'txn/123',
        step: 'analyzing',
        message: 'Processing...',
        progress: 50,
        timestamp: '2026-01-24T10:30:00Z',
        status: 'in_progress',
      });

      await authscriptService.getAnalysisStatus('txn/123');

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis/txn%2F123/status',
      });
    });
  });

  describe('downloadFilledForm', () => {
    it('downloadFilledForm_ValidId_ReturnsBlob', async () => {
      const mockBlob = new Blob(['PDF content'], { type: 'application/pdf' });

      // Mock fetch directly for blob response
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        blob: async () => mockBlob,
      });

      const result = await authscriptService.downloadFilledForm('txn-123');

      expect(global.fetch).toHaveBeenCalledWith('/api/analysis/txn-123/form');
      expect(result).toBeInstanceOf(Blob);
    });

    it('downloadFilledForm_EmptyId_ThrowsError', async () => {
      await expect(
        authscriptService.downloadFilledForm('')
      ).rejects.toThrow('transactionId is required');
    });

    it('downloadFilledForm_FailedRequest_ThrowsError', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: false,
        statusText: 'Not Found',
      });

      await expect(
        authscriptService.downloadFilledForm('txn-123')
      ).rejects.toThrow('Failed to download form: Not Found');
    });

    it('downloadFilledForm_UrlEncodesTransactionId', async () => {
      const mockBlob = new Blob(['PDF content'], { type: 'application/pdf' });
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        blob: async () => mockBlob,
      });

      await authscriptService.downloadFilledForm('txn/123');

      expect(global.fetch).toHaveBeenCalledWith('/api/analysis/txn%2F123/form');
    });
  });

  describe('submitToEpic', () => {
    it('submitToEpic_ValidId_ReturnsDocumentId', async () => {
      mockCustomFetch.mockResolvedValueOnce({ documentId: 'doc-789' });

      const result = await authscriptService.submitToEpic('txn-123');

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis/txn-123/submit',
        method: 'POST',
      });
      expect(result.documentId).toBe('doc-789');
    });

    it('submitToEpic_EmptyId_ThrowsError', async () => {
      await expect(
        authscriptService.submitToEpic('')
      ).rejects.toThrow('transactionId is required');
    });

    it('submitToEpic_UrlEncodesTransactionId', async () => {
      mockCustomFetch.mockResolvedValueOnce({ documentId: 'doc-789' });

      await authscriptService.submitToEpic('txn/123');

      expect(mockCustomFetch).toHaveBeenCalledWith({
        url: '/api/analysis/txn%2F123/submit',
        method: 'POST',
      });
    });
  });
});
