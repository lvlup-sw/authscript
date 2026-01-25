import { describe, it, expect } from 'vitest';
// Test that all types are re-exported from index
import type {
  // Common types
  SortDirection,
  PaginationParams,
  PaginatedResponse,
  ApiError,
  ApiResponse,
  // AuthScript types
  PAFormResponse,
  EvidenceItem,
  StatusUpdate,
  // CDS types
  FhirAuthorization,
  CdsContext,
  CdsRequest,
  CdsResponse,
  CdsCard,
  CdsSuggestion,
  CdsAction,
  CdsLink,
} from '../index';

describe('Index Exports', () => {
  describe('Common Types Export', () => {
    it('SortDirection_ExportedFromIndex', () => {
      const dir: SortDirection = 'asc';
      expect(dir).toBe('asc');
    });

    it('PaginationParams_ExportedFromIndex', () => {
      const params: PaginationParams = { pageNumber: 1, pageSize: 20 };
      expect(params.pageNumber).toBe(1);
    });

    it('PaginatedResponse_ExportedFromIndex', () => {
      const response: PaginatedResponse<string> = {
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false,
      };
      expect(response.items).toHaveLength(0);
    });

    it('ApiError_ExportedFromIndex', () => {
      const error: ApiError = { type: 'error', title: 'Error', status: 500 };
      expect(error.status).toBe(500);
    });

    it('ApiResponse_ExportedFromIndex', () => {
      const response: ApiResponse<string> = { success: true, data: 'test' };
      expect(response.success).toBe(true);
    });
  });

  describe('AuthScript Types Export', () => {
    it('PAFormResponse_ExportedFromIndex', () => {
      const response: PAFormResponse = {
        patientName: 'Test',
        patientDob: '2000-01-01',
        memberId: 'M123',
        diagnosisCodes: [],
        procedureCode: '99213',
        clinicalSummary: '',
        supportingEvidence: [],
        recommendation: 'APPROVE',
        confidenceScore: 1,
        fieldMappings: {},
      };
      expect(response.recommendation).toBe('APPROVE');
    });

    it('EvidenceItem_ExportedFromIndex', () => {
      const item: EvidenceItem = {
        criterionId: 'C1',
        status: 'MET',
        evidence: 'test',
        source: 'test.pdf',
        confidence: 0.9,
      };
      expect(item.status).toBe('MET');
    });

    it('StatusUpdate_ExportedFromIndex', () => {
      const update: StatusUpdate = {
        transactionId: 'T1',
        step: 'step1',
        message: 'msg',
        progress: 50,
        timestamp: '2025-01-01',
        status: 'in_progress',
      };
      expect(update.status).toBe('in_progress');
    });
  });

  describe('CDS Types Export', () => {
    it('FhirAuthorization_ExportedFromIndex', () => {
      const auth: FhirAuthorization = {
        accessToken: 'token',
        tokenType: 'Bearer',
        expiresIn: 3600,
      };
      expect(auth.tokenType).toBe('Bearer');
    });

    it('CdsContext_ExportedFromIndex', () => {
      const context: CdsContext = { patientId: 'P123' };
      expect(context.patientId).toBe('P123');
    });

    it('CdsRequest_ExportedFromIndex', () => {
      const request: CdsRequest = {
        hookInstance: 'uuid',
        hook: 'order-select',
        context: { patientId: 'P123' },
      };
      expect(request.hook).toBe('order-select');
    });

    it('CdsResponse_ExportedFromIndex', () => {
      const response: CdsResponse = { cards: [] };
      expect(response.cards).toHaveLength(0);
    });

    it('CdsCard_ExportedFromIndex', () => {
      const card: CdsCard = {
        summary: 'Test',
        indicator: 'info',
        source: { label: 'Test' },
      };
      expect(card.indicator).toBe('info');
    });

    it('CdsSuggestion_ExportedFromIndex', () => {
      const suggestion: CdsSuggestion = { label: 'Test' };
      expect(suggestion.label).toBe('Test');
    });

    it('CdsAction_ExportedFromIndex', () => {
      const action: CdsAction = { type: 'create' };
      expect(action.type).toBe('create');
    });

    it('CdsLink_ExportedFromIndex', () => {
      const link: CdsLink = {
        label: 'Test',
        url: 'https://example.com',
        type: 'absolute',
      };
      expect(link.type).toBe('absolute');
    });
  });
});
