namespace CasinoApp.Domain;

public sealed class Account
{
    public const int MaxNameLength = 50;

    private Account() { }

    public Guid Id { get; private set; }
    public string PlayerName { get; private set; } = null!;
    public long BalanceCents { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Account Create(string playerName, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        PlayerName = NormalizeName(playerName),
        BalanceCents = 0,
        CreatedAt = now
    };

    public static string NormalizeName(string name) => name.Trim().ToLowerInvariant();

    public Result<LedgerEntry> Deposit(long amountCents, string reference, DateTimeOffset now)
    {
        if (amountCents <= 0) return WalletError.InvalidAmount;

        BalanceCents += amountCents;
        return LedgerEntry.Create(Id, LedgerEntryType.Deposit, amountCents, BalanceCents, reference, now);
    }

    public Result<LedgerEntry> Withdraw(long amountCents, string reference, DateTimeOffset now)
    {
        if (amountCents <= 0) return WalletError.InvalidAmount;
        if (amountCents > BalanceCents) return WalletError.InsufficientFunds;

        BalanceCents -= amountCents;
        return LedgerEntry.Create(Id, LedgerEntryType.Withdrawal, -amountCents, BalanceCents, reference, now);
    }

    public Result<IReadOnlyList<LedgerEntry>> SettleRound(
        Guid roundId, long betCents, long winCents, DateTimeOffset now)
    {
        if (betCents <= 0 || winCents < 0) return WalletError.InvalidAmount;
        if (betCents > BalanceCents) return WalletError.InsufficientFunds;

        var reference = roundId.ToString();
        var entries = new List<LedgerEntry>(2);

        BalanceCents -= betCents;
        entries.Add(LedgerEntry.Create(Id, LedgerEntryType.Bet, -betCents, BalanceCents, reference, now));

        if (winCents > 0)
        {
            BalanceCents += winCents;
            entries.Add(LedgerEntry.Create(Id, LedgerEntryType.Win, winCents, BalanceCents, reference, now));
        }

        return entries;
    }
}