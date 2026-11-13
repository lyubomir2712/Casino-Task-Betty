namespace CasinoApp.Domain;

public sealed class LedgerEntry
{
    private LedgerEntry() { }

    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public LedgerEntryType Type { get; private set; }
    public long AmountCents { get; private set; }
    public long BalanceAfterCents { get; private set; }
    public string Reference { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    internal static LedgerEntry Create(
        Guid accountId, LedgerEntryType type, long amountCents,
        long balanceAfterCents, string reference, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        AccountId = accountId,
        Type = type,
        AmountCents = amountCents,
        BalanceAfterCents = balanceAfterCents,
        Reference = reference,
        CreatedAt = now
    };
}