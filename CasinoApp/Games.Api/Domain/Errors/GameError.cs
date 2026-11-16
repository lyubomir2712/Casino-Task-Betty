namespace Games.Api.Domain.Errors;

public enum GameError
{
    InvalidBet,
    InvalidStateTransition
}

public readonly record struct Result<T>(T? Value, GameError? Error)
{
    public bool IsSuccess => Error is null;

    public static implicit operator Result<T>(T value) => new(value, null);
    public static implicit operator Result<T>(GameError error) => new(default, error);
}