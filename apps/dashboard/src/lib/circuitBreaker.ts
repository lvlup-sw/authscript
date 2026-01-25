/**
 * Circuit Breaker Implementation
 *
 * Protects against cascading failures by tracking external service health.
 * State machine: CLOSED → OPEN → HALF_OPEN → CLOSED
 *
 * When OPEN, returns cached data if available or throws CircuitOpenError.
 */

export type CircuitState = "CLOSED" | "OPEN" | "HALF_OPEN";

export interface CircuitBreakerOptions {
  name: string;
  failureThreshold: number; // Failures before opening (default: 5)
  resetTimeout: number; // Ms before transitioning to half-open (default: 30000)
  successThreshold?: number; // Successes needed to close from half-open (default: 2)
}

export class CircuitOpenError extends Error {
  constructor(
    public readonly serviceName: string,
    public readonly retryAfter: number
  ) {
    super(`Circuit breaker for ${serviceName} is OPEN. Retry after ${retryAfter}ms`);
    this.name = "CircuitOpenError";
  }
}

export class CircuitBreaker {
  private state: CircuitState = "CLOSED";
  private failureCount = 0;
  private successCount = 0;
  private lastFailureTime = 0;

  private readonly name: string;
  private readonly failureThreshold: number;
  private readonly resetTimeout: number;
  private readonly successThreshold: number;

  constructor(options: CircuitBreakerOptions) {
    this.name = options.name;
    this.failureThreshold = options.failureThreshold;
    this.resetTimeout = options.resetTimeout;
    this.successThreshold = options.successThreshold ?? 2;
  }

  /**
   * Execute a function with circuit breaker protection
   */
  async execute<T>(fn: () => Promise<T>): Promise<T> {
    // Guard: Check if circuit is open
    if (this.state === "OPEN") {
      if (!this.shouldProbe()) {
        const retryAfter = this.getRetryAfter();
        throw new CircuitOpenError(this.name, retryAfter);
      }
      this.transitionTo("HALF_OPEN");
    }

    // Execute the function
    try {
      const result = await fn();
      this.onSuccess();
      return result;
    } catch (error) {
      this.onFailure();
      throw error;
    }
  }

  /**
   * Check if circuit breaker is open
   */
  isOpen(): boolean {
    return this.state === "OPEN";
  }

  /**
   * Get current state
   */
  getState(): CircuitState {
    return this.state;
  }

  /**
   * Get time until next retry is allowed (when open)
   */
  getRetryAfter(): number {
    if (this.state !== "OPEN") return 0;

    const elapsed = Date.now() - this.lastFailureTime;
    return Math.max(0, this.resetTimeout - elapsed);
  }

  /**
   * Get circuit breaker stats for monitoring
   */
  getStats(): {
    name: string;
    state: CircuitState;
    failureCount: number;
    successCount: number;
    lastFailureTime: number;
  } {
    return {
      name: this.name,
      state: this.state,
      failureCount: this.failureCount,
      successCount: this.successCount,
      lastFailureTime: this.lastFailureTime,
    };
  }

  /**
   * Should we allow a probe request (when open)?
   */
  private shouldProbe(): boolean {
    const elapsed = Date.now() - this.lastFailureTime;
    return elapsed >= this.resetTimeout;
  }

  /**
   * Handle successful operation
   */
  private onSuccess(): void {
    if (this.state === "HALF_OPEN") {
      this.successCount++;
      if (this.successCount >= this.successThreshold) {
        this.transitionTo("CLOSED");
        this.failureCount = 0;
        this.successCount = 0;
      }
    } else if (this.state === "CLOSED") {
      // Reset failure count on success in closed state
      this.failureCount = 0;
    }
  }

  /**
   * Handle failed operation
   */
  private onFailure(): void {
    this.lastFailureTime = Date.now();
    this.failureCount++;

    if (this.state === "HALF_OPEN") {
      // Any failure in half-open immediately reopens
      this.transitionTo("OPEN");
      this.successCount = 0;
    } else if (this.state === "CLOSED" && this.failureCount >= this.failureThreshold) {
      this.transitionTo("OPEN");
    }
  }

  /**
   * Transition to a new state with logging
   */
  private transitionTo(newState: CircuitState): void {
    const oldState = this.state;
    this.state = newState;
    console.warn(`[CIRCUIT] ${this.name}: ${oldState} → ${newState}`);
  }
}
