namespace Gateway.API.Abstractions;

public enum ErrorType
{
    None = 0,
    NotFound = 404,
    Validation = 400,
    Conflict = 409,
    Unauthorized = 401,
    Forbidden = 403,
    Infrastructure = 503,
    Unexpected = 500
}
