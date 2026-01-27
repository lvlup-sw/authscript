namespace Gateway.API.Tests.Abstractions;

using Gateway.API.Abstractions;

public class ErrorFactoryTests
{
    [Test]
    public async Task NotFound_ReturnsCorrectError()
    {
        var error = ErrorFactory.NotFound("Patient", "123");

        await Assert.That(error.Code).IsEqualTo("Patient.NotFound");
        await Assert.That(error.Message).IsEqualTo("Patient/123 not found");
        await Assert.That(error.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task Validation_ReturnsCorrectError()
    {
        var error = ErrorFactory.Validation("Invalid input");

        await Assert.That(error.Code).IsEqualTo("Validation.Failed");
        await Assert.That(error.Message).IsEqualTo("Invalid input");
        await Assert.That(error.Type).IsEqualTo(ErrorType.Validation);
    }

    [Test]
    public async Task Unauthorized_WithDefaultMessage_ReturnsCorrectError()
    {
        var error = ErrorFactory.Unauthorized();

        await Assert.That(error.Code).IsEqualTo("Auth.Unauthorized");
        await Assert.That(error.Message).IsEqualTo("Authentication required");
        await Assert.That(error.Type).IsEqualTo(ErrorType.Unauthorized);
    }

    [Test]
    public async Task Unauthorized_WithCustomMessage_ReturnsCorrectError()
    {
        var error = ErrorFactory.Unauthorized("Token expired");

        await Assert.That(error.Code).IsEqualTo("Auth.Unauthorized");
        await Assert.That(error.Message).IsEqualTo("Token expired");
        await Assert.That(error.Type).IsEqualTo(ErrorType.Unauthorized);
    }

    [Test]
    public async Task Infrastructure_WithoutInner_ReturnsCorrectError()
    {
        var error = ErrorFactory.Infrastructure("Service down");

        await Assert.That(error.Code).IsEqualTo("Infrastructure.Error");
        await Assert.That(error.Message).IsEqualTo("Service down");
        await Assert.That(error.Type).IsEqualTo(ErrorType.Infrastructure);
        await Assert.That(error.Inner).IsNull();
    }

    [Test]
    public async Task Infrastructure_WithInner_IncludesInnerException()
    {
        var inner = new Exception("network error");
        var error = ErrorFactory.Infrastructure("Service down", inner);

        await Assert.That(error.Inner).IsEqualTo(inner);
        await Assert.That(error.Type).IsEqualTo(ErrorType.Infrastructure);
    }

    [Test]
    public async Task Unexpected_WithoutInner_ReturnsCorrectError()
    {
        var error = ErrorFactory.Unexpected("Something went wrong");

        await Assert.That(error.Code).IsEqualTo("Unexpected.Error");
        await Assert.That(error.Message).IsEqualTo("Something went wrong");
        await Assert.That(error.Type).IsEqualTo(ErrorType.Unexpected);
        await Assert.That(error.Inner).IsNull();
    }

    [Test]
    public async Task Unexpected_WithInner_IncludesInnerException()
    {
        var inner = new InvalidOperationException("bad state");
        var error = ErrorFactory.Unexpected("Something went wrong", inner);

        await Assert.That(error.Inner).IsEqualTo(inner);
        await Assert.That(error.Type).IsEqualTo(ErrorType.Unexpected);
    }
}
