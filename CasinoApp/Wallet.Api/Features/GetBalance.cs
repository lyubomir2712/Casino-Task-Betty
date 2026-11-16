using CasinoApp.Domain;
using CasinoApp.Features.Common;
using CasinoApp.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CasinoApp.Features;

public static class GetBalance
{
    public sealed record Response(Guid PlayerId, decimal Balance);

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/players/{playerId:guid}/balance", Handle);

    private static async Task<Results<Ok<Response>, ProblemHttpResult>> Handle(
        Guid playerId, WalletDbContext db, CancellationToken ct)
    {
        var balanceCents = await db.Accounts.AsNoTracking()
            .Where(a => a.Id == playerId)
            .Select(a => (long?)a.BalanceCents)
            .SingleOrDefaultAsync(ct);

        return balanceCents is null
            ? WalletError.PlayerNotFound.ToProblem()
            : TypedResults.Ok(new Response(playerId, Money.ToDollars(balanceCents.Value)));
    }
}