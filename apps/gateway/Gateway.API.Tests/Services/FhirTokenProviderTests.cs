using Gateway.API.Contracts.Http;
using Gateway.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Gateway.API.Tests.Services;

/// <summary>
/// Tests for <see cref="FhirTokenProvider"/>.
/// </summary>
public sealed class FhirTokenProviderTests
{
    private readonly ITokenStrategyResolver _resolver = Substitute.For<ITokenStrategyResolver>();
    private readonly ITokenAcquisitionStrategy _strategy = Substitute.For<ITokenAcquisitionStrategy>();

    [Test]
    public async Task GetTokenAsync_WithValidStrategy_ReturnsToken()
    {
        // Arrange
        _resolver.Resolve().Returns(_strategy);
        _strategy.AcquireTokenAsync(Arg.Any<CancellationToken>()).Returns("test-access-token");

        var provider = new FhirTokenProvider(_resolver, NullLogger<FhirTokenProvider>.Instance);

        // Act
        var token = await provider.GetTokenAsync();

        // Assert
        await Assert.That(token).IsEqualTo("test-access-token");
    }

    [Test]
    public async Task GetTokenAsync_WhenStrategyReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        _resolver.Resolve().Returns(_strategy);
        _strategy.AcquireTokenAsync(Arg.Any<CancellationToken>()).Returns((string?)null);

        var provider = new FhirTokenProvider(_resolver, NullLogger<FhirTokenProvider>.Instance);

        // Act
        InvalidOperationException? exception = null;
        try
        {
            await provider.GetTokenAsync();
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // Assert
        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Message).Contains("FHIR access token");
    }

    [Test]
    public async Task GetTokenAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _resolver.Resolve().Returns(_strategy);
        _strategy.AcquireTokenAsync(cts.Token).Returns("token");

        var provider = new FhirTokenProvider(_resolver, NullLogger<FhirTokenProvider>.Instance);

        // Act
        await provider.GetTokenAsync(cts.Token);

        // Assert
        await _strategy.Received(1).AcquireTokenAsync(cts.Token);
    }
}
