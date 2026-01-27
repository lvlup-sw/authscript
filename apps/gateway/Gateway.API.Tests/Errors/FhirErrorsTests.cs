namespace Gateway.API.Tests.Errors;

using Gateway.API.Abstractions;
using Gateway.API.Errors;

public class FhirErrorsTests
{
    [Test]
    public async Task ServiceUnavailable_HasCorrectCode()
    {
        await Assert.That(FhirErrors.ServiceUnavailable.Code).IsEqualTo("Fhir.ServiceUnavailable");
    }

    [Test]
    public async Task ServiceUnavailable_HasCorrectType()
    {
        await Assert.That(FhirErrors.ServiceUnavailable.Type).IsEqualTo(ErrorType.Infrastructure);
    }

    [Test]
    public async Task Timeout_HasCorrectCode()
    {
        await Assert.That(FhirErrors.Timeout.Code).IsEqualTo("Fhir.Timeout");
    }

    [Test]
    public async Task Timeout_HasCorrectType()
    {
        await Assert.That(FhirErrors.Timeout.Type).IsEqualTo(ErrorType.Infrastructure);
    }

    [Test]
    public async Task AuthenticationFailed_HasCorrectCode()
    {
        await Assert.That(FhirErrors.AuthenticationFailed.Code).IsEqualTo("Fhir.AuthFailed");
    }

    [Test]
    public async Task AuthenticationFailed_HasCorrectType()
    {
        await Assert.That(FhirErrors.AuthenticationFailed.Type).IsEqualTo(ErrorType.Unauthorized);
    }

    [Test]
    public async Task NotFound_ReturnsCorrectError()
    {
        var error = FhirErrors.NotFound("Patient", "123");

        await Assert.That(error.Code).IsEqualTo("Patient.NotFound");
        await Assert.That(error.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task InvalidResponse_IncludesDetails()
    {
        var error = FhirErrors.InvalidResponse("missing resourceType");

        await Assert.That(error.Code).IsEqualTo("Fhir.InvalidResponse");
        await Assert.That(error.Message).Contains("missing resourceType");
        await Assert.That(error.Type).IsEqualTo(ErrorType.Infrastructure);
    }

    [Test]
    public async Task NetworkError_WithoutInner_ReturnsCorrectError()
    {
        var error = FhirErrors.NetworkError("Connection failed");

        await Assert.That(error.Code).IsEqualTo("Fhir.NetworkError");
        await Assert.That(error.Message).IsEqualTo("Connection failed");
        await Assert.That(error.Type).IsEqualTo(ErrorType.Infrastructure);
        await Assert.That(error.Inner).IsNull();
    }

    [Test]
    public async Task NetworkError_WithInner_IncludesInnerException()
    {
        var inner = new HttpRequestException("timeout");
        var error = FhirErrors.NetworkError("Connection failed", inner);

        await Assert.That(error.Inner).IsEqualTo(inner);
        await Assert.That(error.Type).IsEqualTo(ErrorType.Infrastructure);
    }
}
