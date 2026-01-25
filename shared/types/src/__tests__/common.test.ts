import { describe, it, expect } from 'vitest';
import type {
  SortDirection,
  PaginationParams,
  PaginatedResponse,
  ApiError,
  ApiResponse,
} from '../common';

describe('Common Types', () => {
  describe('PaginatedResponse', () => {
    it('PaginatedResponse_WithItems_HasRequiredFields', () => {
      const response: PaginatedResponse<string> = {
        items: ['a', 'b', 'c'],
        totalCount: 100,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 5,
        hasNextPage: true,
        hasPreviousPage: false,
      };
      expect(response.items).toHaveLength(3);
      expect(response.totalPages).toBe(5);
    });
  });

  describe('ApiResponse', () => {
    it('ApiResponse_Success_HasDataAndNoError', () => {
      const response: ApiResponse<{ name: string }> = {
        data: { name: 'test' },
        success: true,
      };
      expect(response.success).toBe(true);
      expect(response.data?.name).toBe('test');
    });

    it('ApiResponse_Error_HasErrorAndNoData', () => {
      const response: ApiResponse<never> = {
        error: {
          type: 'validation_error',
          title: 'Validation Failed',
          status: 400,
          detail: 'Invalid input',
        },
        success: false,
      };
      expect(response.success).toBe(false);
      expect(response.error?.status).toBe(400);
    });
  });

  describe('SortDirection', () => {
    it('SortDirection_ValidValues_AcceptsAscDesc', () => {
      const asc: SortDirection = 'asc';
      const desc: SortDirection = 'desc';
      expect(['asc', 'desc']).toContain(asc);
      expect(['asc', 'desc']).toContain(desc);
    });
  });

  describe('PaginationParams', () => {
    it('PaginationParams_WithRequired_HasPageFields', () => {
      const params: PaginationParams = {
        pageNumber: 1,
        pageSize: 20,
      };
      expect(params.pageNumber).toBe(1);
      expect(params.pageSize).toBe(20);
    });

    it('PaginationParams_WithOptional_HasSortDirection', () => {
      const params: PaginationParams = {
        pageNumber: 2,
        pageSize: 50,
        sortDirection: 'desc',
      };
      expect(params.sortDirection).toBe('desc');
    });
  });

  describe('ApiError', () => {
    it('ApiError_WithRequired_HasCoreFields', () => {
      const error: ApiError = {
        type: 'validation_error',
        title: 'Validation Failed',
        status: 400,
      };
      expect(error.type).toBe('validation_error');
      expect(error.status).toBe(400);
    });

    it('ApiError_WithOptional_HasDetailAndInstance', () => {
      const error: ApiError = {
        type: 'not_found',
        title: 'Resource Not Found',
        status: 404,
        detail: 'The requested patient was not found',
        instance: '/api/patients/123',
      };
      expect(error.detail).toBe('The requested patient was not found');
      expect(error.instance).toBe('/api/patients/123');
    });
  });
});
