using Games.Api.Domain;
using Games.Api.Infrastructure.Wallet;
using Games.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Games.Api.Features.PlaceBet;

public static class PlaceBet
{
    public sealed record Request(Guid PlayerId, decimal Amount);
    public sealed record Response(Guid RoundId, string Status, decimal Bet, decimal Win, decimal? Balance);

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/bets", Handle);

    private static async Task<Results<Ok<Response>, Accepted<Response>, ValidationProblem, ProblemHttpResult>> Handle(
        Request request,
        GamesDbContext db,
        OutcomeCalculator calculator,
        WalletClient wallet,
        TimeProvider time,
        CancellationToken ct)
    {
        if (!Money.HasValidPrecision(request.Amount))
            return InvalidBet();

        var created = Round.Create(request.PlayerId, Money.ToCents(request.Amount), calculator, time.GetUtcNow());
        if (!created.IsSuccess) return InvalidBet();

        var round = created.Value!;
        db.Rounds.Add(round);
        await db.SaveChangesAsync(ct);

        var settle = await wallet.SettleRoundAsync(round, ct);

        switch (settle.Status)
        {
            case SettleStatus.Settled:
                round.MarkSettled(settle.BalanceCents, time.GetUtcNow());
                break;
            case SettleStatus.Rejected:
                round.MarkRejected(time.GetUtcNow());
                break;
            case SettleStatus.Unavailable:
                return TypedResults.Accepted($"/bets/{round.Id}", ToResponse(round));
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            round = await db.Rounds.AsNoTracking().SingleAsync(r => r.Id == round.Id, ct);
        }

        return round.Status switch
        {
            RoundStatus.Settled  => TypedResults.Ok(ToResponse(round)),
            RoundStatus.Rejected => TypedResults.Problem("Bet rejected: insufficient funds or unknown player.", statusCode: 422),
            _                    => TypedResults.Accepted($"/bets/{round.Id}", ToResponse(round))
        };
    }

    private static Response ToResponse(Round round) => new(
        round.Id,
        round.Status.ToString(),
        Money.ToDollars(round.BetCents),
        Money.ToDollars(round.WinCents),
        round.BalanceAfterCents is { } balance ? Money.ToDollars(balance) : null);

    private static ValidationProblem InvalidBet() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["amount"] = [$"Bet must be between ${Money.ToDollars(BetLimits.MinCents)} and ${Money.ToDollars(BetLimits.MaxCents)} with at most 2 decimal places."]
        });
}