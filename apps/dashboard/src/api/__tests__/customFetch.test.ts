import { describe, it, expect, vi, beforeEach } from 'vitest';
import { customFetch, ApiError } from '../customFetch';

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

describe('customFetch', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
  });

  describe('successful requests', () => {
    it('customFetch_SuccessfulGet_ReturnsData', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ data: 'test' }),
        headers: new Headers(),
      });

      const result = await customFetch({ url: '/api/test' });
      expect(result).toEqual({ data: 'test' });
    });

    it('customFetch_204NoContent_ReturnsUndefined', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 204,
        headers: new Headers(),
      });

      const result = await customFetch({ url: '/api/test', method: 'DELETE' });
      expect(result).toBeUndefined();
    });

    it('customFetch_205ResetContent_ReturnsUndefined', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 205,
        headers: new Headers(),
      });

      const result = await customFetch({ url: '/api/test', method: 'POST' });
      expect(result).toBeUndefined();
    });

    it('customFetch_PostWithBody_SetsContentType', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ id: 1 }),
        headers: new Headers(),
      });

      await customFetch({
        url: '/api/test',
        method: 'POST',
        body: JSON.stringify({ name: 'test' }),
      });

      expect(mockFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.any(Headers),
        })
      );

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Content-Type')).toBe('application/json');
    });

    it('customFetch_GetWithoutBody_DoesNotSetContentType', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test' });

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Content-Type')).toBeNull();
    });

    it('customFetch_AlwaysSetsAcceptHeader', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test' });

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Accept')).toBe('application/json');
    });
  });

  describe('error handling', () => {
    it('customFetch_Non2xxResponse_ThrowsApiError', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        json: async () => ({
          message: 'Validation failed',
          details: { field: 'name' },
        }),
        headers: new Headers(),
      });

      await expect(customFetch({ url: '/api/test' })).rejects.toThrow(ApiError);
    });

    it('customFetch_ApiError_IncludesStatusAndMessage', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        json: async () => ({ message: 'Not found' }),
        headers: new Headers(),
      });

      try {
        await customFetch({ url: '/api/test' });
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError);
        expect((error as ApiError).status).toBe(404);
        expect((error as ApiError).message).toBe('Not found');
      }
    });

    it('customFetch_ApiError_IncludesDetails', async () => {
      const details = { field: 'email', code: 'invalid_format' };
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 422,
        json: async () => ({ message: 'Validation failed', details }),
        headers: new Headers(),
      });

      try {
        await customFetch({ url: '/api/test' });
        expect.fail('Should have thrown');
      } catch (error) {
        expect((error as ApiError).details).toEqual(details);
      }
    });

    it('customFetch_NonJsonError_UsesDefaultMessage', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => {
          throw new Error('Not JSON');
        },
        headers: new Headers(),
      });

      try {
        await customFetch({ url: '/api/test' });
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError);
        expect((error as ApiError).message).toContain('500');
      }
    });

    it('customFetch_ErrorWithEmptyMessage_UsesDefaultMessage', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 503,
        json: async () => ({ message: '' }),
        headers: new Headers(),
      });

      try {
        await customFetch({ url: '/api/test' });
        expect.fail('Should have thrown');
      } catch (error) {
        expect((error as ApiError).message).toContain('503');
      }
    });
  });

  describe('traceId extraction', () => {
    it('customFetch_ResponseWithTraceId_IncludesInError', async () => {
      const headers = new Headers();
      headers.set('x-trace-id', 'trace-12345');

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({ message: 'Server error' }),
        headers,
      });

      try {
        await customFetch({ url: '/api/test' });
        expect.fail('Should have thrown');
      } catch (error) {
        expect((error as ApiError).traceId).toBe('trace-12345');
      }
    });

    it('customFetch_ResponseWithoutTraceId_TraceIdIsUndefined', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({ message: 'Server error' }),
        headers: new Headers(),
      });

      try {
        await customFetch({ url: '/api/test' });
        expect.fail('Should have thrown');
      } catch (error) {
        expect((error as ApiError).traceId).toBeUndefined();
      }
    });
  });

  describe('authentication', () => {
    it('customFetch_WithStoredToken_AddsAuthorizationHeader', async () => {
      sessionStorage.setItem(
        'smart_session',
        JSON.stringify({
          accessToken: 'test-token-123',
        })
      );

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test' });

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Authorization')).toBe('Bearer test-token-123');
    });

    it('customFetch_NoStoredToken_OmitsAuthorizationHeader', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test' });

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Authorization')).toBeNull();
    });

    it('customFetch_InvalidJsonInStorage_OmitsAuthorizationHeader', async () => {
      sessionStorage.setItem('smart_session', 'not-valid-json');

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test' });

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Authorization')).toBeNull();
    });

    it('customFetch_SessionWithoutAccessToken_OmitsAuthorizationHeader', async () => {
      sessionStorage.setItem('smart_session', JSON.stringify({ userId: '123' }));

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test' });

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Authorization')).toBeNull();
    });
  });

  describe('request configuration', () => {
    it('customFetch_IncludesCredentials', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test' });

      const callArgs = mockFetch.mock.calls[0];
      expect(callArgs[1].credentials).toBe('include');
    });

    it('customFetch_PassesThroughMethod', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test', method: 'PUT' });

      const callArgs = mockFetch.mock.calls[0];
      expect(callArgs[1].method).toBe('PUT');
    });

    it('customFetch_PassesThroughBody', async () => {
      const body = JSON.stringify({ name: 'test' });
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test', method: 'POST', body });

      const callArgs = mockFetch.mock.calls[0];
      expect(callArgs[1].body).toBe(body);
    });

    it('customFetch_PreservesCustomHeaders', async () => {
      const customHeaders = new Headers();
      customHeaders.set('X-Custom-Header', 'custom-value');

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
        headers: new Headers(),
      });

      await customFetch({ url: '/api/test', headers: customHeaders });

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('X-Custom-Header')).toBe('custom-value');
    });
  });
});

describe('ApiError', () => {
  it('ApiError_HasCorrectName', () => {
    const error = new ApiError(500, 'Server error');
    expect(error.name).toBe('ApiError');
  });

  it('ApiError_ExtendsError', () => {
    const error = new ApiError(500, 'Server error');
    expect(error).toBeInstanceOf(Error);
  });

  it('ApiError_StoresAllProperties', () => {
    const error = new ApiError(400, 'Bad request', { field: 'name' }, 'trace-123');
    expect(error.status).toBe(400);
    expect(error.message).toBe('Bad request');
    expect(error.details).toEqual({ field: 'name' });
    expect(error.traceId).toBe('trace-123');
  });

  it('ApiError_AllowsOptionalProperties', () => {
    const error = new ApiError(404, 'Not found');
    expect(error.details).toBeUndefined();
    expect(error.traceId).toBeUndefined();
  });
});
