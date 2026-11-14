using CasinoApp.Domain;
using CasinoApp.Features.Common;
using CasinoApp.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CasinoApp.Features.Deposit;

public static class Deposit
{
    public sealed record Request(decimal Amount);
    public sealed record Response(decimal Amount, decimal Balance);

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/players/{playerId:guid}/deposit", Handle);

    private static async Task<Results<Ok<Response>, ValidationProblem, ProblemHttpResult>> Handle(
        Guid playerId,
        Request request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        WalletDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var errors = RequestValidation.ValidateAmountAndKey(request.Amount, idempotencyKey);
        if (errors is not null) return TypedResults.ValidationProblem(errors);

        var amountCents = Money.ToCents(request.Amount);

        var result = await AccountOperations.ExecuteAsync(
            db, playerId, LedgerEntryType.Deposit, idempotencyKey!,
            account => account.Deposit(amountCents, idempotencyKey!, time.GetUtcNow()),
            ct);

        if (!result.IsSuccess) return result.Error!.Value.ToProblem();

        var entry = result.Value!;
        return TypedResults.Ok(new Response(
            Money.ToDollars(entry.AmountCents),
            Money.ToDollars(entry.BalanceAfterCents)));
    }
}