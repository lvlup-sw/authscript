using Gateway.API.Contracts.Http;

namespace Gateway.API.Services.Http;

/// <summary>
/// Resolves the appropriate token acquisition strategy based on configuration.
/// Iterates through registered strategies and returns the first one that can handle
/// the current provider configuration.
/// </summary>
public sealed class TokenStrategyResolver
{
    private readonly IEnumerable<ITokenAcquisitionStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenStrategyResolver"/> class.
    /// </summary>
    /// <param name="strategies">All registered token acquisition strategies.</param>
    public TokenStrategyResolver(IEnumerable<ITokenAcquisitionStrategy> strategies)
    {
        _strategies = strategies;
    }

    /// <summary>
    /// Resolves the appropriate token acquisition strategy.
    /// </summary>
    /// <returns>The first strategy where CanHandle is true, or null if none can handle.</returns>
    public ITokenAcquisitionStrategy? Resolve()
    {
        return _strategies.FirstOrDefault(s => s.CanHandle);
    }
}
