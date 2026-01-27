namespace Gateway.API.Tests.Abstractions;

using Gateway.API.Abstractions;

public class ErrorTests
{
    [Test]
    public async Task Error_Constructor_SetsAllProperties()
    {
        var inner = new Exception("inner");
        var error = new Error("CODE", "Message", ErrorType.NotFound) { Inner = inner };

        await Assert.That(error.Code).IsEqualTo("CODE");
        await Assert.That(error.Message).IsEqualTo("Message");
        await Assert.That(error.Type).IsEqualTo(ErrorType.NotFound);
        await Assert.That(error.Inner).IsEqualTo(inner);
    }

    [Test]
    public async Task Error_DefaultType_IsUnexpected()
    {
        var error = new Error("CODE", "Message");
        await Assert.That(error.Type).IsEqualTo(ErrorType.Unexpected);
    }

    [Test]
    public async Task Error_Inner_DefaultsToNull()
    {
        var error = new Error("CODE", "Message");
        await Assert.That(error.Inner).IsNull();
    }

    [Test]
    public async Task Error_Equality_WorksCorrectly()
    {
        var error1 = new Error("CODE", "Message", ErrorType.NotFound);
        var error2 = new Error("CODE", "Message", ErrorType.NotFound);
        var error3 = new Error("DIFFERENT", "Message", ErrorType.NotFound);

        await Assert.That(error1).IsEqualTo(error2);
        await Assert.That(error1).IsNotEqualTo(error3);
    }
}

public class ErrorTypeTests
{
    [Test]
    public async Task ErrorType_NotFound_MatchesHttpStatusCode()
    {
        await Assert.That((int)ErrorType.NotFound).IsEqualTo(404);
    }

    [Test]
    public async Task ErrorType_Validation_MatchesHttpStatusCode()
    {
        await Assert.That((int)ErrorType.Validation).IsEqualTo(400);
    }

    [Test]
    public async Task ErrorType_Unauthorized_MatchesHttpStatusCode()
    {
        await Assert.That((int)ErrorType.Unauthorized).IsEqualTo(401);
    }

    [Test]
    public async Task ErrorType_Forbidden_MatchesHttpStatusCode()
    {
        await Assert.That((int)ErrorType.Forbidden).IsEqualTo(403);
    }

    [Test]
    public async Task ErrorType_Conflict_MatchesHttpStatusCode()
    {
        await Assert.That((int)ErrorType.Conflict).IsEqualTo(409);
    }

    [Test]
    public async Task ErrorType_Infrastructure_MatchesHttpStatusCode()
    {
        await Assert.That((int)ErrorType.Infrastructure).IsEqualTo(503);
    }

    [Test]
    public async Task ErrorType_Unexpected_MatchesHttpStatusCode()
    {
        await Assert.That((int)ErrorType.Unexpected).IsEqualTo(500);
    }

    [Test]
    public async Task ErrorType_None_IsZero()
    {
        await Assert.That((int)ErrorType.None).IsEqualTo(0);
    }
}
