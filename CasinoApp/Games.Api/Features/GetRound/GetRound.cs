using Games.Api.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Games.Api.Features.GetRound;

public static class GetRound
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/bets/{roundId:guid}", Handle);

    private static async Task<Results<Ok<PlaceBet.PlaceBet.Response>, NotFound>> Handle(
        Guid roundId, GamesDbContext db, CancellationToken ct)
    {
        var round = await db.Rounds.AsNoTracking().SingleOrDefaultAsync(r => r.Id == roundId, ct);

        return round is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(PlaceBet.PlaceBet.ToResponse(round));
    }
}