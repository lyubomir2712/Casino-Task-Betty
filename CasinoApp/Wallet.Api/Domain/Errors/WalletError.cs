namespace CasinoApp.Domain;

public enum WalletError
{
    InvalidAmount,
    InsufficientFunds,
    PlayerNotFound,
    IdempotencyKeyReused,
    ConcurrencyConflict
}

public readonly record struct Result<T>(T? Value, WalletError? Error)
{
    public bool IsSuccess => Error is null;

    public static implicit operator Result<T>(T value) => new(value, null);
    public static implicit operator Result<T>(WalletError error) => new(default, error);
}