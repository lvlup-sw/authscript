/**
 * Custom fetch wrapper for Orval-generated API clients.
 * Handles authentication, error handling, and request/response transformation.
 *
 * @module api/customFetch
 */

import { getAuthToken } from './auth';

/**
 * Options for customFetch, extending standard RequestInit with URL
 */
interface CustomFetchOptions extends RequestInit {
  /** The URL to fetch */
  url: string;
}

/**
 * Shape of error responses from the API
 */
interface ErrorResponse {
  /** Human-readable error message */
  message: string;
  /** HTTP status code */
  status: number;
  /** Additional error details (validation errors, etc.) */
  details?: unknown;
  /** Request trace ID for debugging */
  traceId?: string;
}

/**
 * Custom API error with status code, message, details, and optional trace ID.
 * Thrown when the API returns a non-2xx response.
 *
 * @example
 * ```typescript
 * try {
 *   await customFetch({ url: '/api/resource' });
 * } catch (error) {
 *   if (error instanceof ApiError) {
 *     console.error(`Error ${error.status}: ${error.message}`);
 *     if (error.traceId) {
 *       console.error(`Trace ID: ${error.traceId}`);
 *     }
 *   }
 * }
 * ```
 */
export class ApiError extends Error {
  /**
   * Creates a new ApiError
   * @param status - HTTP status code
   * @param message - Human-readable error message
   * @param details - Additional error details (validation errors, etc.)
   * @param traceId - Request trace ID from x-trace-id header
   */
  constructor(
    public readonly status: number,
    message: string,
    public readonly details?: unknown,
    public readonly traceId?: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * Custom fetch wrapper with error handling, auth, and trace ID support.
 * Automatically adds authentication headers from SMART on FHIR session storage,
 * sets appropriate content headers, and extracts trace IDs for debugging.
 *
 * @typeParam T - Expected response type
 * @param options - Fetch options including url
 * @returns Parsed JSON response
 * @throws ApiError on non-2xx responses with status, message, details, and traceId
 *
 * @example
 * ```typescript
 * // GET request
 * const data = await customFetch<User>({ url: '/api/users/1' });
 *
 * // POST request with body
 * const created = await customFetch<User>({
 *   url: '/api/users',
 *   method: 'POST',
 *   body: JSON.stringify({ name: 'John' }),
 * });
 * ```
 */
export async function customFetch<T>(options: CustomFetchOptions): Promise<T> {
  const { url, ...fetchOptions } = options;

  const headers = new Headers(fetchOptions.headers);

  // Add default headers
  if (!headers.has('Content-Type') && fetchOptions.body) {
    headers.set('Content-Type', 'application/json');
  }
  headers.set('Accept', 'application/json');

  // Add auth token if available (for SMART on FHIR)
  const token = getStoredAccessToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(url, {
    ...fetchOptions,
    headers,
    credentials: 'include',
  });

  // Extract trace ID for debugging
  const traceId = response.headers.get('x-trace-id') ?? undefined;

  if (!response.ok) {
    let errorMessage = `Request failed with status ${response.status}`;
    let details: unknown = undefined;

    try {
      const errorBody = (await response.json()) as ErrorResponse;
      errorMessage = errorBody.message || errorMessage;
      details = errorBody.details;
    } catch {
      // Response wasn't JSON, use default message
    }

    throw new ApiError(response.status, errorMessage, details, traceId);
  }

  // Handle empty responses
  if (response.status === 204 || response.status === 205 || response.headers.get('Content-Length') === '0') {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

/**
 * Retrieves the stored access token from session storage.
 * Delegates to shared getAuthToken() which checks authscript_session first,
 * then falls back to smart_session (SMART on FHIR).
 *
 * @returns Access token if found and valid, null otherwise
 */
function getStoredAccessToken(): string | null {
  return getAuthToken();
}

export default customFetch;
