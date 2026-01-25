/**
 * Global Rate Limiter Implementation
 *
 * Sliding window rate limiter using in-memory timestamp array.
 * Protects backend services from overload.
 *
 * Note: This is a global limiter (not per-user) - all requests share the same quota.
 */

export interface RateLimiterOptions {
  name: string;
  maxRequests: number; // Max requests in window
  windowMs: number; // Window size in milliseconds
}

export class RateLimiter {
  private requests: number[] = [];

  private readonly name: string;
  private readonly maxRequests: number;
  private readonly windowMs: number;

  constructor(options: RateLimiterOptions) {
    this.name = options.name;
    this.maxRequests = options.maxRequests;
    this.windowMs = options.windowMs;
  }

  /**
   * Check if request is allowed and consume quota
   * @returns true if allowed, false if rate limited
   */
  allow(): boolean {
    const now = Date.now();

    // Clean old requests outside window
    this.cleanup(now);

    // Guard: Under limit - allow request
    if (this.requests.length < this.maxRequests) {
      this.requests.push(now);
      return true;
    }

    // Over limit
    console.warn(`[RATELIMIT] ${this.name}: Rate limit exceeded (${this.requests.length}/${this.maxRequests})`);
    return false;
  }

  /**
   * Get remaining requests in current window
   */
  getRemainingRequests(): number {
    this.cleanup(Date.now());
    return Math.max(0, this.maxRequests - this.requests.length);
  }

  /**
   * Get seconds until rate limit resets (oldest request expires)
   */
  getResetTime(): number {
    if (this.requests.length === 0) return 0;

    const now = Date.now();
    const oldestRequest = this.requests[0];
    const expiresAt = oldestRequest + this.windowMs;
    const resetInMs = Math.max(0, expiresAt - now);

    return Math.ceil(resetInMs / 1000);
  }

  /**
   * Get rate limiter stats for monitoring/headers
   */
  getStats(): {
    name: string;
    remaining: number;
    limit: number;
    resetSeconds: number;
  } {
    return {
      name: this.name,
      remaining: this.getRemainingRequests(),
      limit: this.maxRequests,
      resetSeconds: this.getResetTime(),
    };
  }

  /**
   * Remove timestamps outside the current window
   */
  private cleanup(now: number): void {
    const windowStart = now - this.windowMs;
    this.requests = this.requests.filter((timestamp) => timestamp > windowStart);
  }
}
