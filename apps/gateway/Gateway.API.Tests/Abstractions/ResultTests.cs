namespace Gateway.API.Tests.Abstractions;

using Gateway.API.Abstractions;

public class ResultTests
{
    [Test]
    public async Task Result_Success_ContainsValue()
    {
        var result = Result<string>.Success("test");
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo("test");
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task Result_Failure_ContainsError()
    {
        var error = new Error("TEST", "Test error", ErrorType.Validation);
        var result = Result<string>.Failure(error);
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(error);
    }

    [Test]
    public async Task Result_Match_ExecutesCorrectBranch()
    {
        var success = Result<int>.Success(42);
        var failure = Result<int>.Failure(new Error("E", "err"));

        var successResult = success.Match(v => $"ok:{v}", e => $"fail:{e.Code}");
        var failureResult = failure.Match(v => $"ok:{v}", e => $"fail:{e.Code}");

        await Assert.That(successResult).IsEqualTo("ok:42");
        await Assert.That(failureResult).IsEqualTo("fail:E");
    }

    [Test]
    public async Task Result_Map_TransformsSuccessValue()
    {
        var success = Result<int>.Success(5);
        var failure = Result<int>.Failure(new Error("E", "err"));

        var mappedSuccess = success.Map(x => x * 2);
        var mappedFailure = failure.Map(x => x * 2);

        await Assert.That(mappedSuccess.Value).IsEqualTo(10);
        await Assert.That(mappedFailure.IsFailure).IsTrue();
    }

    [Test]
    public async Task Result_ImplicitConversion_FromValue()
    {
        Result<string> result = "implicit value";
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo("implicit value");
    }

    [Test]
    public async Task Result_ImplicitConversion_FromError()
    {
        var error = new Error("E", "err");
        Result<string> result = error;
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(error);
    }
}
