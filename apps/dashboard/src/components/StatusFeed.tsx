import { useEffect, useState, useCallback, useRef } from 'react';
import { authscriptService } from '../api/authscriptService';
import { cn } from '@/lib/utils';
import type { StatusUpdate } from '@authscript/types';

interface StatusFeedProps {
  transactionId?: string;
  pollingInterval?: number;
  onComplete?: (transactionId: string) => void;
  className?: string;
}

type FeedStatus = 'idle' | 'loading' | 'polling' | 'completed' | 'error';

/**
 * Real-time status feed component for analysis progress
 * Polls the status endpoint and displays processing steps
 */
export function StatusFeed({
  transactionId,
  pollingInterval = 2000,
  onComplete,
  className,
}: StatusFeedProps) {
  const [status, setStatus] = useState<StatusUpdate | null>(null);
  const [feedStatus, setFeedStatus] = useState<FeedStatus>('idle');
  const [error, setError] = useState<string | null>(null);
  const onCompleteRef = useRef(onComplete);
  const shouldPollRef = useRef(false);

  // Keep onComplete ref up to date
  useEffect(() => {
    onCompleteRef.current = onComplete;
  }, [onComplete]);

  const fetchStatus = useCallback(async () => {
    if (!transactionId) return;

    try {
      const result = await authscriptService.getAnalysisStatus(transactionId);
      setStatus(result);
      setError(null);

      if (result.status === 'completed') {
        setFeedStatus('completed');
        shouldPollRef.current = false;
        onCompleteRef.current?.(transactionId);
      } else if (result.status === 'error') {
        setFeedStatus('error');
        shouldPollRef.current = false;
      } else {
        setFeedStatus('polling');
        shouldPollRef.current = true;
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch status');
      setFeedStatus('error');
      shouldPollRef.current = false;
    }
  }, [transactionId]);

  // Initial fetch when transactionId changes
  useEffect(() => {
    if (!transactionId) {
      setFeedStatus('idle');
      setStatus(null);
      shouldPollRef.current = false;
      return;
    }

    setFeedStatus('loading');
    fetchStatus();
  }, [transactionId, fetchStatus]);

  // Polling interval - uses ref to avoid re-triggering effect on feedStatus change
  useEffect(() => {
    if (!transactionId) return;

    const pollInterval = setInterval(() => {
      if (shouldPollRef.current) {
        fetchStatus();
      }
    }, pollingInterval);

    return () => clearInterval(pollInterval);
  }, [transactionId, pollingInterval, fetchStatus]);

  // Idle state - no transaction
  if (!transactionId) {
    return (
      <div className={cn('p-4 text-muted-foreground', className)}>
        No active analysis
      </div>
    );
  }

  // Loading state
  if (feedStatus === 'loading' && !status) {
    return (
      <div className={cn('p-4', className)}>
        <div className="flex items-center gap-2">
          <div className="animate-spin h-4 w-4 border-2 border-primary border-t-transparent rounded-full" />
          <span>Loading status...</span>
        </div>
      </div>
    );
  }

  // Error state (only when no status data)
  if (feedStatus === 'error' && error && !status) {
    return (
      <div className={cn('p-4 text-destructive', className)}>
        <div className="flex items-center gap-2">
          <span className="text-lg">X</span>
          <span>{error}</span>
        </div>
      </div>
    );
  }

  // Main status display
  return (
    <div className={cn('p-4 space-y-4', className)}>
      {/* Status header */}
      <div className="flex items-center gap-2">
        {feedStatus === 'completed' ? (
          <span className="text-lg text-[hsl(var(--success))]">OK</span>
        ) : feedStatus === 'error' || status?.status === 'error' ? (
          <span className="text-lg text-destructive">X</span>
        ) : (
          <div className="animate-spin h-4 w-4 border-2 border-primary border-t-transparent rounded-full" />
        )}
        <span className="font-medium">{status?.step || 'Processing'}</span>
      </div>

      {/* Message */}
      {status?.message && (
        <p className="text-muted-foreground">{status.message}</p>
      )}

      {/* Progress bar */}
      {status && status.status !== 'error' && (
        <div className="space-y-1">
          <div
            role="progressbar"
            aria-valuenow={status.progress}
            aria-valuemin={0}
            aria-valuemax={100}
            className="h-2 bg-muted rounded-full overflow-hidden"
          >
            <div
              className={cn(
                'h-full transition-all duration-300',
                feedStatus === 'completed' ? 'bg-[hsl(var(--success))]' : 'bg-primary'
              )}
              style={{ width: `${status.progress}%` }}
            />
          </div>
          <p className="text-xs text-muted-foreground text-right">
            {status.progress}%
          </p>
        </div>
      )}

      {/* Timestamp */}
      {status?.timestamp && (
        <p className="text-xs text-muted-foreground">
          Last updated: {new Date(status.timestamp).toLocaleTimeString()}
        </p>
      )}
    </div>
  );
}

export default StatusFeed;
