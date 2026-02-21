import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import React from 'react';

// Mock the graphql client
vi.mock('../graphqlClient', () => ({
  graphqlClient: {
    request: vi.fn(),
  },
}));

import { graphqlClient } from '../graphqlClient';
import {
  useApprovePARequest,
  useDenyPARequest,
  useProcessPARequest,
  useConnectionStatus,
  QUERY_KEYS,
} from '../graphqlService';

const mockRequest = vi.mocked(graphqlClient.request);

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client: queryClient }, children);
}

describe('useApprovePARequest', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should call approvePARequest mutation with correct id', async () => {
    mockRequest.mockResolvedValueOnce({
      approvePARequest: { id: 'PA-001', status: 'approved' },
    });

    const { result } = renderHook(() => useApprovePARequest(), {
      wrapper: createWrapper(),
    });

    result.current.mutate('PA-001');

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockRequest).toHaveBeenCalledOnce();
    // Verify the mutation string contains approvePARequest
    const callArgs = mockRequest.mock.calls[0];
    expect(callArgs[0]).toContain('approvePARequest');
  });
});

describe('useDenyPARequest', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should call denyPARequest mutation with id and reason', async () => {
    mockRequest.mockResolvedValueOnce({
      denyPARequest: { id: 'PA-001', status: 'denied' },
    });

    const { result } = renderHook(() => useDenyPARequest(), {
      wrapper: createWrapper(),
    });

    result.current.mutate({ id: 'PA-001', reason: 'Missing documentation' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockRequest).toHaveBeenCalledOnce();
    const callArgs = mockRequest.mock.calls[0] as unknown[];
    expect(callArgs[0]).toContain('denyPARequest');
    expect(callArgs[1]).toEqual({ id: 'PA-001', reason: 'Missing documentation' });
  });
});

describe('useProcessPARequest', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should call processPARequest mutation with correct id', async () => {
    mockRequest.mockResolvedValueOnce({
      processPARequest: { id: 'PA-001', status: 'ready' },
    });

    const { result } = renderHook(() => useProcessPARequest(), {
      wrapper: createWrapper(),
    });

    result.current.mutate('PA-001');

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockRequest).toHaveBeenCalledOnce();
    const callArgs = mockRequest.mock.calls[0];
    expect(callArgs[0]).toContain('processPARequest');
    expect(callArgs[1]).toEqual({ id: 'PA-001' });
  });

  it('useProcessPARequest_InvalidatesCorrectQueries_OnSuccess', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });

    // Seed the cache with data for each query key we expect to be invalidated
    queryClient.setQueryData(QUERY_KEYS.paRequests, []);
    queryClient.setQueryData(QUERY_KEYS.paRequest('PA-002'), { id: 'PA-002' });
    queryClient.setQueryData(QUERY_KEYS.paStats, { ready: 0 });
    queryClient.setQueryData(QUERY_KEYS.activity, []);

    const wrapper = ({ children }: { children: React.ReactNode }) =>
      React.createElement(QueryClientProvider, { client: queryClient }, children);

    mockRequest.mockResolvedValueOnce({
      processPARequest: { id: 'PA-002', status: 'ready' },
    });

    const { result } = renderHook(() => useProcessPARequest(), { wrapper });

    // Spy on invalidateQueries
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    result.current.mutate('PA-002');

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // Verify all four query keys are invalidated
    const invalidatedKeys = invalidateSpy.mock.calls.map(call => call[0]?.queryKey);
    expect(invalidatedKeys).toContainEqual(QUERY_KEYS.paRequests);
    expect(invalidatedKeys).toContainEqual(QUERY_KEYS.paRequest('PA-002'));
    expect(invalidatedKeys).toContainEqual(QUERY_KEYS.paStats);
    expect(invalidatedKeys).toContainEqual(QUERY_KEYS.activity);
  });
});

describe('useConnectionStatus', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should return connected when paStats query succeeds', async () => {
    mockRequest.mockResolvedValueOnce({
      paStats: { ready: 3, submitted: 1, waitingForInsurance: 2, attention: 1, total: 7 },
    });

    const { result } = renderHook(() => useConnectionStatus(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.connected).toBe(true));
    expect(result.current.error).toBeUndefined();
  });

  it('should return disconnected when paStats query fails', async () => {
    // Reject both initial attempt and the single retry (retry: 1)
    mockRequest.mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useConnectionStatus(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.connected).toBe(false), { timeout: 5000 });
    expect(result.current.error).toBeDefined();
  });
});
