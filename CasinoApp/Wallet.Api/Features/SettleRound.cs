using CasinoApp.Domain;
using CasinoApp.Features.Common;
using CasinoApp.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CasinoApp.Features;

public static class SettleRound
{
    public sealed record Request(Guid PlayerId, Guid RoundId, long BetCents, long WinCents);
    public sealed record Response(Guid RoundId, long BalanceCents);

    private const int MaxAttempts = 3;

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/internal/rounds", Handle);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> Handle(
        Request request, WalletDbContext db, TimeProvider time, CancellationToken ct)
    {
        var reference = request.RoundId.ToString();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var existing = await db.LedgerEntries.AsNoTracking()
                .Where(e => e.Reference == reference &&
                            (e.Type == LedgerEntryType.Bet || e.Type == LedgerEntryType.Win))
                .ToListAsync(ct);

            if (existing.Count > 0)
            {
                var bet = existing.Single(e => e.Type == LedgerEntryType.Bet);

                if (bet.AccountId != request.PlayerId || -bet.AmountCents != request.BetCents)
                    return WalletError.IdempotencyKeyReused.ToProblem();

                var last = existing.SingleOrDefault(e => e.Type == LedgerEntryType.Win) ?? bet;
                return TypedResults.Ok(new Response(request.RoundId, last.BalanceAfterCents));
            }

            var account = await db.Accounts.SingleOrDefaultAsync(a => a.Id == request.PlayerId, ct);
            if (account is null) return WalletError.PlayerNotFound.ToProblem();

            var result = account.SettleRound(request.RoundId, request.BetCents, request.WinCents, time.GetUtcNow());
            if (!result.IsSuccess) return result.Error!.Value.ToProblem();

            db.LedgerEntries.AddRange(result.Value!);

            try
            {
                await db.SaveChangesAsync(ct);
                return TypedResults.Ok(new Response(request.RoundId, account.BalanceCents));
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                   { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                db.ChangeTracker.Clear();
            }
        }

        return WalletError.ConcurrencyConflict.ToProblem();
    }
}