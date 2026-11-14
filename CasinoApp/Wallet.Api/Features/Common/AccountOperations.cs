using CasinoApp.Domain;
using CasinoApp.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CasinoApp.Features.Common;

public static class AccountOperations
{
    private const int MaxAttempts = 3;

    public static async Task<Result<LedgerEntry>> ExecuteAsync(
        WalletDbContext db,
        Guid playerId,
        LedgerEntryType type,
        string reference,
        Func<Account, Result<LedgerEntry>> operation,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var existing = await db.LedgerEntries.AsNoTracking()
                .SingleOrDefaultAsync(e => e.Type == type && e.Reference == reference, ct);

            if (existing is not null)
                return existing.AccountId == playerId ? existing : WalletError.IdempotencyKeyReused;

            var account = await db.Accounts.SingleOrDefaultAsync(a => a.Id == playerId, ct);
            if (account is null) return WalletError.PlayerNotFound;

            var result = operation(account);
            if (!result.IsSuccess) return result;

            db.LedgerEntries.Add(result.Value!);

            try
            {
                await db.SaveChangesAsync(ct);
                return result;
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

        return WalletError.ConcurrencyConflict;
    }
}