using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.API.Services;

/// <summary>
/// Validates API keys using a HashSet for O(1) lookup.
/// </summary>
public sealed class ApiKeyValidator : IApiKeyValidator
{
    private readonly HashSet<string> _validKeys;
    private readonly ILogger<ApiKeyValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyValidator"/> class.
    /// </summary>
    /// <param name="options">API key settings.</param>
    /// <param name="logger">Logger instance.</param>
    public ApiKeyValidator(IOptions<ApiKeySettings> options, ILogger<ApiKeyValidator> logger)
    {
        _validKeys = [.. options.Value.ValidApiKeys];
        _logger = logger;

        _logger.LogInformation("ApiKeyValidator initialized with {Count} API key(s)", _validKeys.Count);

        if (_validKeys.Count == 0)
        {
            _logger.LogWarning("ApiKeyValidator initialized with no API keys. Authentication will fail for all requests");
        }
    }

    /// <inheritdoc />
    public bool IsValid(string? apiKey)
    {
        return !string.IsNullOrWhiteSpace(apiKey) && _validKeys.Contains(apiKey);
    }
}
