namespace Gateway.API.Configuration;

/// <summary>
/// Configuration for HTTP resilience policies.
/// </summary>
public sealed class ResiliencyOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Resilience";

    /// <summary>
    /// Maximum retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Base delay between retries in seconds.
    /// </summary>
    public double RetryDelaySeconds { get; init; } = 1.0;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Circuit breaker failure threshold.
    /// </summary>
    public int CircuitBreakerThreshold { get; init; } = 5;

    /// <summary>
    /// Circuit breaker break duration in seconds.
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; init; } = 30;
}
