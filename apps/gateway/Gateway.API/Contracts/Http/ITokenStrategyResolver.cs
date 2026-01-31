namespace Gateway.API.Contracts.Http;

/// <summary>
/// Resolves the appropriate token acquisition strategy for the current request.
/// </summary>
public interface ITokenStrategyResolver
{
    /// <summary>
    /// Gets the token acquisition strategy for the current context.
    /// </summary>
    /// <returns>The appropriate token acquisition strategy.</returns>
    ITokenAcquisitionStrategy Resolve();
}
