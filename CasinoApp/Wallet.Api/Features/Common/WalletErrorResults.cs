using CasinoApp.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CasinoApp.Features.Common;

public static class WalletErrorResults
{
    public static ProblemHttpResult ToProblem(this WalletError error) => error switch
    {
        WalletError.InvalidAmount        => TypedResults.Problem("Amount must be positive.", statusCode: 400),
        WalletError.PlayerNotFound       => TypedResults.Problem("Player not found.", statusCode: 404),
        WalletError.IdempotencyKeyReused => TypedResults.Problem("Idempotency key already used for a different request.", statusCode: 409),
        WalletError.ConcurrencyConflict  => TypedResults.Problem("Account is busy, please retry.", statusCode: 409),
        WalletError.InsufficientFunds    => TypedResults.Problem("Insufficient funds.", statusCode: 422),
        _                                => TypedResults.Problem(statusCode: 500)
    };
}