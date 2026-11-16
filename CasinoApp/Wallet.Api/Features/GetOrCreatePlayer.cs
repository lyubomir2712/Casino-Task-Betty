using CasinoApp.Domain;
using CasinoApp.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CasinoApp.Features;

public static class GetOrCreatePlayer
{
    public sealed record Request(string Name);
    public sealed record Response(Guid PlayerId, string Name, decimal Balance);

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/players", Handle);

    private static async Task<Results<Ok<Response>, ValidationProblem>> Handle(
        Request request, WalletDbContext db, TimeProvider time, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > Account.MaxNameLength)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = [$"Name is required and must be at most {Account.MaxNameLength} characters."]
            });
        }

        var name = Account.NormalizeName(request.Name);

        var account = await db.Accounts.AsNoTracking()
            .SingleOrDefaultAsync(a => a.PlayerName == name, ct);

        if (account is null)
        {
            account = Account.Create(name, time.GetUtcNow());
            db.Accounts.Add(account);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                                   { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                db.ChangeTracker.Clear();
                account = await db.Accounts.AsNoTracking().SingleAsync(a => a.PlayerName == name, ct);
            }
        }

        return TypedResults.Ok(new Response(account.Id, account.PlayerName, Money.ToDollars(account.BalanceCents)));
    }
}