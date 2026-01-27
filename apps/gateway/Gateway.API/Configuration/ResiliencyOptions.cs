namespace Gateway.API.Configuration;

/// <summary>
/// Configuration options for HTTP resilience policies.
/// </summary>
public sealed class ResiliencyOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Resiliency";

    /// <summary>
    /// Maximum retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Initial delay between retries in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; init; } = 500;

    /// <summary>
    /// Circuit breaker failure threshold.
    /// </summary>
    public int CircuitBreakerThreshold { get; init; } = 5;

    /// <summary>
    /// Circuit breaker break duration in seconds.
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; init; } = 30;
}
