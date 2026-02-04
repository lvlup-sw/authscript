using Gateway.API.Contracts;
using Gateway.API.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace Gateway.API.Tests.Filters;

/// <summary>
/// Tests for <see cref="ApiKeyEndpointFilter"/>.
/// </summary>
public sealed class ApiKeyEndpointFilterTests
{
    private const string ApiKeyHeader = "X-API-Key";
    private readonly IApiKeyValidator _validator = Substitute.For<IApiKeyValidator>();

    private static DefaultHttpContext CreateHttpContext(string? apiKey = null)
    {
        var context = new DefaultHttpContext();
        if (apiKey is not null)
        {
            context.Request.Headers[ApiKeyHeader] = apiKey;
        }
        return context;
    }

    private static EndpointFilterInvocationContext CreateFilterContext(HttpContext httpContext)
    {
        return new DefaultEndpointFilterInvocationContext(httpContext);
    }

    private static EndpointFilterDelegate CreateNextDelegate(object? result = null)
    {
        return _ => new ValueTask<object?>(result ?? Results.Ok());
    }

    [Test]
    public async Task InvokeAsync_WithValidKey_CallsNext()
    {
        // Arrange
        var httpContext = CreateHttpContext("valid-key");
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;
        var next = new EndpointFilterDelegate(_ =>
        {
            nextCalled = true;
            return new ValueTask<object?>(Results.Ok());
        });

        _validator.IsValid("valid-key").Returns(true);
        var filter = new ApiKeyEndpointFilter(_validator);

        // Act
        var result = await filter.InvokeAsync(filterContext, next);

        // Assert
        await Assert.That(nextCalled).IsTrue();
        await Assert.That(result).IsTypeOf<Ok>();
    }

    [Test]
    public async Task InvokeAsync_WithMissingKey_Returns401()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var filterContext = CreateFilterContext(httpContext);
        var next = CreateNextDelegate();

        var filter = new ApiKeyEndpointFilter(_validator);

        // Act
        var result = await filter.InvokeAsync(filterContext, next);

        // Assert
        await Assert.That(result).IsTypeOf<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result!;
        await Assert.That(problem.StatusCode).IsEqualTo(401);
    }

    [Test]
    public async Task InvokeAsync_WithInvalidKey_Returns401()
    {
        // Arrange
        var httpContext = CreateHttpContext("invalid-key");
        var filterContext = CreateFilterContext(httpContext);
        var next = CreateNextDelegate();

        _validator.IsValid("invalid-key").Returns(false);
        var filter = new ApiKeyEndpointFilter(_validator);

        // Act
        var result = await filter.InvokeAsync(filterContext, next);

        // Assert
        await Assert.That(result).IsTypeOf<ProblemHttpResult>();
        var problem = (ProblemHttpResult)result!;
        await Assert.That(problem.StatusCode).IsEqualTo(401);
    }

    [Test]
    public async Task InvokeAsync_WithEmptyKey_Returns401()
    {
        // Arrange
        var httpContext = CreateHttpContext(string.Empty);
        var filterContext = CreateFilterContext(httpContext);
        var next = CreateNextDelegate();

        var filter = new ApiKeyEndpointFilter(_validator);

        // Act
        var result = await filter.InvokeAsync(filterContext, next);

        // Assert
        await Assert.That(result).IsTypeOf<ProblemHttpResult>();
    }

    [Test]
    public async Task InvokeAsync_WithMissingKey_DoesNotCallNext()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;
        var next = new EndpointFilterDelegate(_ =>
        {
            nextCalled = true;
            return new ValueTask<object?>(Results.Ok());
        });

        var filter = new ApiKeyEndpointFilter(_validator);

        // Act
        await filter.InvokeAsync(filterContext, next);

        // Assert
        await Assert.That(nextCalled).IsFalse();
    }

    [Test]
    public async Task InvokeAsync_WithValidKey_DoesNotCallValidator_ForMissingHeader()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var filterContext = CreateFilterContext(httpContext);
        var next = CreateNextDelegate();

        var filter = new ApiKeyEndpointFilter(_validator);

        // Act
        await filter.InvokeAsync(filterContext, next);

        // Assert
        _validator.DidNotReceive().IsValid(Arg.Any<string?>());
    }
}
