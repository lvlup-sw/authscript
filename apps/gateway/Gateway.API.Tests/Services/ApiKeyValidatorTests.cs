using Gateway.API.Configuration;
using Gateway.API.Contracts;
using Gateway.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for <see cref="ApiKeyValidator"/>.
/// </summary>
public sealed class ApiKeyValidatorTests
{
    private static ApiKeyValidator CreateValidator(params string[] validKeys)
    {
        var settings = new ApiKeySettings { ValidApiKeys = [.. validKeys] };
        var options = Options.Create(settings);
        return new ApiKeyValidator(options, NullLogger<ApiKeyValidator>.Instance);
    }

    [Test]
    public async Task IsValid_WithValidKey_ReturnsTrue()
    {
        // Arrange
        var validator = CreateValidator("test-key-1", "test-key-2");

        // Act
        var result = validator.IsValid("test-key-1");

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsValid_WithInvalidKey_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator("test-key-1");

        // Act
        var result = validator.IsValid("invalid-key");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator("test-key-1");

        // Act
        var result = validator.IsValid(null);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithEmptyKey_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator("test-key-1");

        // Act
        var result = validator.IsValid(string.Empty);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithWhitespaceKey_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator("test-key-1");

        // Act
        var result = validator.IsValid("   ");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_IsCaseSensitive()
    {
        // Arrange
        var validator = CreateValidator("Test-Key");

        // Act
        var result = validator.IsValid("test-key");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValid_WithNoConfiguredKeys_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var result = validator.IsValid("any-key");

        // Assert
        await Assert.That(result).IsFalse();
    }
}
