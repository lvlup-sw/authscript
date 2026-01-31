using System.Text;
using Gateway.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Gateway.API.Tests.Middleware;

/// <summary>
/// Tests for CdsHookTokenMiddleware.
/// </summary>
public class CdsHookTokenMiddlewareTests
{
    private readonly ILogger<CdsHookTokenMiddleware> _logger;
    private bool _nextCalled;

    public CdsHookTokenMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<CdsHookTokenMiddleware>>();
        _nextCalled = false;
    }

    private RequestDelegate CreateNextDelegate()
    {
        return _ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        };
    }

    [Test]
    public async Task CdsHookTokenMiddleware_InvokeAsync_ExtractsTokenFromFhirAuthorization()
    {
        // Arrange
        var context = CreateHttpContext("/cds-hooks/test", "POST", """
            {
                "hook": "order-select",
                "fhirAuthorization": {
                    "access_token": "extracted-fhir-token",
                    "token_type": "Bearer",
                    "expires_in": 3600
                }
            }
            """);

        var sut = new CdsHookTokenMiddleware(CreateNextDelegate(), _logger);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        await Assert.That(context.Items.ContainsKey("FhirAccessToken")).IsTrue();
        await Assert.That(context.Items["FhirAccessToken"]).IsEqualTo("extracted-fhir-token");
    }

    [Test]
    public async Task CdsHookTokenMiddleware_InvokeAsync_SetsHttpContextItem()
    {
        // Arrange
        const string expectedToken = "my-test-token-123";
        var context = CreateHttpContext("/cds-hooks/order-sign", "POST", $$"""
            {
                "hook": "order-sign",
                "fhirAuthorization": {
                    "access_token": "{{expectedToken}}"
                }
            }
            """);

        var sut = new CdsHookTokenMiddleware(CreateNextDelegate(), _logger);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        await Assert.That(context.Items["FhirAccessToken"]).IsEqualTo(expectedToken);
        await Assert.That(_nextCalled).IsTrue();
    }

    [Test]
    public async Task CdsHookTokenMiddleware_InvokeAsync_SkipsNonCdsHooksRequests()
    {
        // Arrange
        var context = CreateHttpContext("/api/analysis", "POST", """
            {
                "fhirAuthorization": {
                    "access_token": "should-not-extract"
                }
            }
            """);

        var sut = new CdsHookTokenMiddleware(CreateNextDelegate(), _logger);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        await Assert.That(context.Items.ContainsKey("FhirAccessToken")).IsFalse();
        await Assert.That(_nextCalled).IsTrue();
    }

    [Test]
    public async Task CdsHookTokenMiddleware_InvokeAsync_SkipsGetRequests()
    {
        // Arrange
        var context = CreateHttpContext("/cds-hooks/services", "GET", "");

        var sut = new CdsHookTokenMiddleware(CreateNextDelegate(), _logger);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        await Assert.That(context.Items.ContainsKey("FhirAccessToken")).IsFalse();
        await Assert.That(_nextCalled).IsTrue();
    }

    [Test]
    public async Task CdsHookTokenMiddleware_InvokeAsync_HandlesNullFhirAuthorization()
    {
        // Arrange
        var context = CreateHttpContext("/cds-hooks/test", "POST", """
            {
                "hook": "order-select",
                "context": {
                    "patientId": "123"
                }
            }
            """);

        var sut = new CdsHookTokenMiddleware(CreateNextDelegate(), _logger);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        await Assert.That(context.Items.ContainsKey("FhirAccessToken")).IsFalse();
        await Assert.That(_nextCalled).IsTrue();
    }

    [Test]
    public async Task CdsHookTokenMiddleware_InvokeAsync_HandlesInvalidJson()
    {
        // Arrange
        var context = CreateHttpContext("/cds-hooks/test", "POST", "{ invalid json }");

        var sut = new CdsHookTokenMiddleware(CreateNextDelegate(), _logger);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        await Assert.That(context.Items.ContainsKey("FhirAccessToken")).IsFalse();
        await Assert.That(_nextCalled).IsTrue();
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to parse")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static DefaultHttpContext CreateHttpContext(string path, string method, string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;

        if (!string.IsNullOrEmpty(body))
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
            context.Request.ContentType = "application/json";
        }

        return context;
    }
}
