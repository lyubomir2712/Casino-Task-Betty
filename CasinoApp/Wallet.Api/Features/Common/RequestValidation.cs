using CasinoApp.Domain;

namespace CasinoApp.Features.Common;

public static class RequestValidation
{
    public const decimal MaxAmount = 1_000_000m;
    public const int MaxIdempotencyKeyLength = 100;

    public static Dictionary<string, string[]>? ValidateAmountAndKey(decimal amount, string? idempotencyKey)
    {
        var errors = new Dictionary<string, string[]>();

        if (amount <= 0 || amount > MaxAmount || !Money.HasValidPrecision(amount))
            errors["amount"] = [$"Amount must be between 0.01 and {MaxAmount} with at most 2 decimal places."];

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > MaxIdempotencyKeyLength)
            errors["Idempotency-Key"] = [$"Header is required (max {MaxIdempotencyKeyLength} characters)."];

        return errors.Count == 0 ? null : errors;
    }
}