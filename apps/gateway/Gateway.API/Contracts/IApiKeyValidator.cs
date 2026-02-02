namespace Gateway.API.Contracts;

/// <summary>
/// Validates API keys for authentication.
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates whether the provided API key is valid.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <returns>True if the API key is valid; otherwise, false.</returns>
    bool IsValid(string? apiKey);
}
