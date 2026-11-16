using Games.Api.Domain;
using Games.Api.Domain.Entities;
using Games.Api.Infrastructure.Wallet;
using Games.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Games.Api.Features.Common;

public sealed class RoundSettler(GamesDbContext db, WalletClient wallet, TimeProvider time)
{
    public async Task<Round> SettleAsync(Round round, CancellationToken ct)
    {
        var result = await wallet.SettleRoundAsync(round, ct);

        switch (result.Status)
        {
            case SettleStatus.Settled:
                round.MarkSettled(result.BalanceCents, time.GetUtcNow());
                break;
            case SettleStatus.Rejected:
                round.MarkRejected(time.GetUtcNow());
                break;
            default:
                return round;
        }

        try
        {
            await db.SaveChangesAsync(ct);
            return round;
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return await db.Rounds.AsNoTracking().SingleAsync(r => r.Id == round.Id, ct);
        }
    }
}