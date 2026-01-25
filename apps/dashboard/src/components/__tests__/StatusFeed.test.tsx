import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { StatusFeed } from '../StatusFeed';
import { authscriptService } from '../../api/authscriptService';

// Mock the service
vi.mock('../../api/authscriptService', () => ({
  authscriptService: {
    getAnalysisStatus: vi.fn(),
  },
}));

const mockGetAnalysisStatus = vi.mocked(authscriptService.getAnalysisStatus);

describe('StatusFeed', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('rendering', () => {
    it('StatusFeed_NoTransactionId_ShowsEmptyState', () => {
      render(<StatusFeed />);
      expect(screen.getByText(/no active analysis/i)).toBeInTheDocument();
    });

    it('StatusFeed_WithTransactionId_ShowsLoading', () => {
      mockGetAnalysisStatus.mockImplementation(() => new Promise(() => {})); // Never resolves
      render(<StatusFeed transactionId="txn-123" />);
      expect(screen.getByText(/loading/i)).toBeInTheDocument();
    });
  });

  describe('status updates', () => {
    it('StatusFeed_ReceivesStatus_DisplaysStep', async () => {
      mockGetAnalysisStatus.mockResolvedValueOnce({
        transactionId: 'txn-123',
        step: 'analyzing',
        message: 'Processing clinical documents...',
        progress: 45,
        timestamp: '2026-01-24T10:30:00Z',
        status: 'in_progress',
      });

      render(<StatusFeed transactionId="txn-123" />);

      // Advance timers to resolve promises - small amount for microtasks
      await act(async () => {
        await vi.advanceTimersByTimeAsync(1);
      });

      expect(screen.getByText(/processing clinical documents/i)).toBeInTheDocument();
    });

    it('StatusFeed_InProgress_ShowsProgressBar', async () => {
      mockGetAnalysisStatus.mockResolvedValueOnce({
        transactionId: 'txn-123',
        step: 'analyzing',
        message: 'Processing...',
        progress: 60,
        timestamp: '2026-01-24T10:30:00Z',
        status: 'in_progress',
      });

      render(<StatusFeed transactionId="txn-123" />);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(1);
      });

      const progressBar = screen.getByRole('progressbar');
      expect(progressBar).toHaveAttribute('aria-valuenow', '60');
    });

    it('StatusFeed_Completed_ShowsSuccessState', async () => {
      mockGetAnalysisStatus.mockResolvedValueOnce({
        transactionId: 'txn-123',
        step: 'complete',
        message: 'Analysis complete',
        progress: 100,
        timestamp: '2026-01-24T10:30:00Z',
        status: 'completed',
      });

      render(<StatusFeed transactionId="txn-123" />);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(1);
      });

      expect(screen.getByText(/analysis complete/i)).toBeInTheDocument();
    });

    it('StatusFeed_Error_ShowsErrorState', async () => {
      mockGetAnalysisStatus.mockResolvedValueOnce({
        transactionId: 'txn-123',
        step: 'error',
        message: 'Failed to process documents',
        progress: 0,
        timestamp: '2026-01-24T10:30:00Z',
        status: 'error',
      });

      render(<StatusFeed transactionId="txn-123" />);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(1);
      });

      expect(screen.getByText(/failed to process/i)).toBeInTheDocument();
    });
  });

  describe('polling', () => {
    it('StatusFeed_InProgress_PollsForUpdates', async () => {
      mockGetAnalysisStatus
        .mockResolvedValueOnce({
          transactionId: 'txn-123',
          step: 'fetching',
          message: 'Fetching data...',
          progress: 20,
          timestamp: '2026-01-24T10:30:00Z',
          status: 'in_progress',
        })
        .mockResolvedValueOnce({
          transactionId: 'txn-123',
          step: 'analyzing',
          message: 'Analyzing...',
          progress: 50,
          timestamp: '2026-01-24T10:30:01Z',
          status: 'in_progress',
        });

      render(<StatusFeed transactionId="txn-123" pollingInterval={1000} />);

      // Wait for initial fetch
      await act(async () => {
        await vi.advanceTimersByTimeAsync(1);
      });

      expect(screen.getByText(/fetching data/i)).toBeInTheDocument();

      // Advance timer for next poll (polling interval)
      await act(async () => {
        await vi.advanceTimersByTimeAsync(1000);
      });

      // Check for the message text specifically
      expect(screen.getByText('Analyzing...')).toBeInTheDocument();
      expect(mockGetAnalysisStatus).toHaveBeenCalledTimes(2);
    });

    it('StatusFeed_Completed_StopsPolling', async () => {
      mockGetAnalysisStatus.mockResolvedValueOnce({
        transactionId: 'txn-123',
        step: 'complete',
        message: 'Done',
        progress: 100,
        timestamp: '2026-01-24T10:30:00Z',
        status: 'completed',
      });

      render(<StatusFeed transactionId="txn-123" pollingInterval={1000} />);

      // Wait for initial fetch
      await act(async () => {
        await vi.advanceTimersByTimeAsync(1);
      });

      expect(screen.getByText(/done/i)).toBeInTheDocument();

      // Advance timer - should not poll again (completed)
      await act(async () => {
        await vi.advanceTimersByTimeAsync(2000);
      });

      expect(mockGetAnalysisStatus).toHaveBeenCalledTimes(1);
    });
  });

  describe('callback', () => {
    it('StatusFeed_OnComplete_CallsCallback', async () => {
      const onComplete = vi.fn();
      mockGetAnalysisStatus.mockResolvedValueOnce({
        transactionId: 'txn-123',
        step: 'complete',
        message: 'Done',
        progress: 100,
        timestamp: '2026-01-24T10:30:00Z',
        status: 'completed',
      });

      render(<StatusFeed transactionId="txn-123" onComplete={onComplete} />);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(1);
      });

      expect(onComplete).toHaveBeenCalledWith('txn-123');
    });
  });
});
