namespace Gateway.API.Abstractions;

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Unexpected)
{
    public Exception? Inner { get; init; }
}
