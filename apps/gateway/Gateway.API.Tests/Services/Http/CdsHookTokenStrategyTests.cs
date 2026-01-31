namespace Gateway.API.Tests.Services.Http;

using Gateway.API.Services.Http;
using Microsoft.AspNetCore.Http;
using NSubstitute;

public class CdsHookTokenStrategyTests
{
    [Test]
    public async Task CdsHookTokenStrategy_CanHandle_ReturnsTrueWhenTokenInContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["FhirAccessToken"] = "test-token";

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var strategy = new CdsHookTokenStrategy(httpContextAccessor);

        // Act
        var canHandle = strategy.CanHandle;

        // Assert
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CdsHookTokenStrategy_CanHandle_ReturnsFalseWhenNoToken()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // No FhirAccessToken in Items

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var strategy = new CdsHookTokenStrategy(httpContextAccessor);

        // Act
        var canHandle = strategy.CanHandle;

        // Assert
        await Assert.That(canHandle).IsFalse();
    }

    [Test]
    public async Task CdsHookTokenStrategy_AcquireTokenAsync_ReturnsTokenFromContext()
    {
        // Arrange
        const string expectedToken = "test-bearer-token";
        var httpContext = new DefaultHttpContext();
        httpContext.Items["FhirAccessToken"] = expectedToken;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var strategy = new CdsHookTokenStrategy(httpContextAccessor);

        // Act
        var token = await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(token).IsEqualTo(expectedToken);
    }

    [Test]
    public async Task CdsHookTokenStrategy_AcquireTokenAsync_ReturnsNullWhenNoContext()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var strategy = new CdsHookTokenStrategy(httpContextAccessor);

        // Act
        var token = await strategy.AcquireTokenAsync();

        // Assert
        await Assert.That(token).IsNull();
    }
}
