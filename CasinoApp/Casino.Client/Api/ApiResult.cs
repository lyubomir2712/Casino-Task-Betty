namespace Casino.Client;

public sealed record ApiResult<T>(T? Value, int StatusCode, string? Error)
{
    public bool IsSuccess => Error is null;
}