import { describe, it, expect } from 'vitest';
import type {
  PAFormResponse,
  EvidenceItem,
  StatusUpdate,
} from '../authscript';

describe('AuthScript Types', () => {
  describe('PAFormResponse', () => {
    it('PAFormResponse_WithAllFields_HasRequiredStructure', () => {
      const response: PAFormResponse = {
        patientName: 'John Doe',
        patientDob: '1990-01-15',
        memberId: 'MBR123456',
        diagnosisCodes: ['E11.9', 'I10'],
        procedureCode: '99213',
        clinicalSummary: 'Patient presents with diabetes',
        supportingEvidence: [],
        recommendation: 'APPROVE',
        confidenceScore: 0.95,
        fieldMappings: { patientName: 'patient.name' },
      };
      expect(response.recommendation).toBe('APPROVE');
      expect(response.confidenceScore).toBe(0.95);
    });

    it('PAFormResponse_Recommendations_AcceptsValidValues', () => {
      const approve: PAFormResponse['recommendation'] = 'APPROVE';
      const needInfo: PAFormResponse['recommendation'] = 'NEED_INFO';
      const manualReview: PAFormResponse['recommendation'] = 'MANUAL_REVIEW';

      expect(['APPROVE', 'NEED_INFO', 'MANUAL_REVIEW']).toContain(approve);
      expect(['APPROVE', 'NEED_INFO', 'MANUAL_REVIEW']).toContain(needInfo);
      expect(['APPROVE', 'NEED_INFO', 'MANUAL_REVIEW']).toContain(manualReview);
    });
  });

  describe('EvidenceItem', () => {
    it('EvidenceItem_WithAllFields_HasRequiredStructure', () => {
      const evidence: EvidenceItem = {
        criterionId: 'CRIT001',
        status: 'MET',
        evidence: 'HbA1c level is 7.2%',
        source: 'lab_results.pdf',
        confidence: 0.92,
      };
      expect(evidence.status).toBe('MET');
      expect(evidence.confidence).toBe(0.92);
    });

    it('EvidenceItem_Status_AcceptsValidValues', () => {
      const met: EvidenceItem['status'] = 'MET';
      const notMet: EvidenceItem['status'] = 'NOT_MET';
      const unclear: EvidenceItem['status'] = 'UNCLEAR';

      expect(['MET', 'NOT_MET', 'UNCLEAR']).toContain(met);
      expect(['MET', 'NOT_MET', 'UNCLEAR']).toContain(notMet);
      expect(['MET', 'NOT_MET', 'UNCLEAR']).toContain(unclear);
    });
  });

  describe('StatusUpdate', () => {
    it('StatusUpdate_WithAllFields_HasRequiredStructure', () => {
      const update: StatusUpdate = {
        transactionId: 'TXN123',
        step: 'document_extraction',
        message: 'Extracting clinical data',
        progress: 45,
        timestamp: '2025-01-21T10:30:00Z',
        status: 'in_progress',
      };
      expect(update.progress).toBe(45);
      expect(update.status).toBe('in_progress');
    });

    it('StatusUpdate_Status_AcceptsValidValues', () => {
      const pending: StatusUpdate['status'] = 'pending';
      const inProgress: StatusUpdate['status'] = 'in_progress';
      const completed: StatusUpdate['status'] = 'completed';
      const error: StatusUpdate['status'] = 'error';

      expect(['pending', 'in_progress', 'completed', 'error']).toContain(pending);
      expect(['pending', 'in_progress', 'completed', 'error']).toContain(inProgress);
      expect(['pending', 'in_progress', 'completed', 'error']).toContain(completed);
      expect(['pending', 'in_progress', 'completed', 'error']).toContain(error);
    });
  });
});
