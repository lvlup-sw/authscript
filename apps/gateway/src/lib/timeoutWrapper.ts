/**
 * Timeout Wrapper for Async Operations
 *
 * Uses AbortController to cancel fetch operations after timeout.
 * Provides clear error messages for debugging.
 */

export class TimeoutError extends Error {
  constructor(
    public readonly operation: string,
    public readonly timeoutMs: number
  ) {
    super(`Operation '${operation}' timed out after ${timeoutMs}ms`);
    this.name = "TimeoutError";
  }
}

/**
 * Execute a function with timeout protection
 *
 * @param fn - Async function that receives an AbortSignal
 * @param timeoutMs - Timeout in milliseconds
 * @param operationName - Name for error messages (optional)
 * @returns Promise with the function result
 * @throws TimeoutError if timeout is exceeded
 *
 * @example
 * const result = await withTimeout(
 *   (signal) => fetch(url, { signal }),
 *   30000,
 *   'FHIR API call'
 * );
 */
export async function withTimeout<T>(
  fn: (signal: AbortSignal) => Promise<T>,
  timeoutMs: number,
  operationName = "operation"
): Promise<T> {
  const controller = new AbortController();
  const { signal } = controller;

  const timeoutId = setTimeout(() => {
    controller.abort();
    console.warn(`[TIMEOUT] ${operationName}: Aborted after ${timeoutMs}ms`);
  }, timeoutMs);

  try {
    const result = await fn(signal);
    return result;
  } catch (error) {
    // Guard: Check if this is an abort error
    if (error instanceof Error && error.name === "AbortError") {
      throw new TimeoutError(operationName, timeoutMs);
    }
    throw error;
  } finally {
    clearTimeout(timeoutId);
  }
}

/**
 * Create a timeout-wrapped fetch function for a specific timeout duration
 *
 * @param timeoutMs - Default timeout in milliseconds
 * @returns A fetch function that automatically applies timeout
 *
 * @example
 * const fetchWithTimeout = createTimeoutFetch(30000);
 * const response = await fetchWithTimeout(url, options);
 */
export function createTimeoutFetch(
  timeoutMs: number
): (url: string | URL, init?: RequestInit) => Promise<Response> {
  return (url: string | URL, init?: RequestInit) =>
    withTimeout(
      (signal) =>
        fetch(url, {
          ...init,
          signal,
        }),
      timeoutMs,
      `fetch ${url.toString()}`
    );
}
