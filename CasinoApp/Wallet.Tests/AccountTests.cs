using CasinoApp.Domain;
using Microsoft.Extensions.Time.Testing;

namespace Wallet.Tests;

public class AccountTests
{
    private static DateTimeOffset At(int year = 2024, int month = 1, int day = 1) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private static Account NewAccount(DateTimeOffset? at = null) =>
        Account.Create("alice", at ?? At());

    // --- Create ---

    [Fact]
    public void Create_UsesProvidedTimestamp()
    {
        var fake = new FakeTimeProvider(At(2024, 6, 15));
        var now = fake.GetUtcNow();
        var account = Account.Create("alice", now);
        Assert.Equal(now, account.CreatedAt);
    }

    [Fact]
    public void Create_StartsWithZeroBalance()
    {
        Assert.Equal(0, NewAccount().BalanceCents);
    }

    // --- Deposit ---

    [Fact]
    public void Deposit_UpdatesBalance()
    {
        var account = NewAccount();
        account.Deposit(1_000, "ref1", At());
        Assert.Equal(1_000, account.BalanceCents);
    }

    [Fact]
    public void Deposit_CreatesCorrectLedgerEntry()
    {
        var account = NewAccount();
        var result = account.Deposit(1_000, "ref1", At());
        Assert.True(result.IsSuccess);
        var entry = result.Value!;
        Assert.Equal(LedgerEntryType.Deposit, entry.Type);
        Assert.Equal(1_000, entry.AmountCents);
        Assert.Equal(1_000, entry.BalanceAfterCents);
        Assert.Equal("ref1", entry.Reference);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deposit_InvalidAmount_ReturnsError(long amountCents)
    {
        var result = NewAccount().Deposit(amountCents, "ref", At());
        Assert.False(result.IsSuccess);
        Assert.Equal(WalletError.InvalidAmount, result.Error);
    }

    // --- Withdraw ---

    [Fact]
    public void Withdraw_UpdatesBalance()
    {
        var account = NewAccount();
        account.Deposit(1_000, "ref1", At());
        account.Withdraw(300, "ref2", At());
        Assert.Equal(700, account.BalanceCents);
    }

    [Fact]
    public void Withdraw_CreatesCorrectLedgerEntry()
    {
        var account = NewAccount();
        account.Deposit(1_000, "ref1", At());
        var result = account.Withdraw(300, "ref2", At());
        Assert.True(result.IsSuccess);
        var entry = result.Value!;
        Assert.Equal(LedgerEntryType.Withdrawal, entry.Type);
        Assert.Equal(-300, entry.AmountCents);
        Assert.Equal(700, entry.BalanceAfterCents);
    }

    [Fact]
    public void Withdraw_OverdraftRefused()
    {
        var account = NewAccount();
        account.Deposit(500, "ref1", At());
        var result = account.Withdraw(600, "ref2", At());
        Assert.False(result.IsSuccess);
        Assert.Equal(WalletError.InsufficientFunds, result.Error);
        Assert.Equal(500, account.BalanceCents);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Withdraw_InvalidAmount_ReturnsError(long amountCents)
    {
        var account = NewAccount();
        account.Deposit(1_000, "ref0", At());
        var result = account.Withdraw(amountCents, "ref1", At());
        Assert.False(result.IsSuccess);
        Assert.Equal(WalletError.InvalidAmount, result.Error);
    }

    // --- SettleRound ---

    [Fact]
    public void SettleRound_Loss_CreatesOneLedgerEntry()
    {
        var account = NewAccount();
        account.Deposit(1_000, "dep", At());
        var result = account.SettleRound(Guid.NewGuid(), 500, 0, At());
        Assert.True(result.IsSuccess);
        var entries = result.Value!;
        Assert.Single(entries);
        Assert.Equal(LedgerEntryType.Bet, entries[0].Type);
        Assert.Equal(-500, entries[0].AmountCents);
    }

    [Fact]
    public void SettleRound_Win_CreatesTwoLedgerEntries()
    {
        var account = NewAccount();
        account.Deposit(1_000, "dep", At());
        var result = account.SettleRound(Guid.NewGuid(), 500, 850, At());
        Assert.True(result.IsSuccess);
        var entries = result.Value!;
        Assert.Equal(2, entries.Count);
        Assert.Equal(LedgerEntryType.Bet, entries[0].Type);
        Assert.Equal(LedgerEntryType.Win, entries[1].Type);
        Assert.Equal(850, entries[1].AmountCents);
    }

    [Fact]
    public void SettleRound_UpdatesBalanceCorrectly()
    {
        var account = NewAccount();
        account.Deposit(1_000, "dep", At());
        account.SettleRound(Guid.NewGuid(), 500, 850, At());
        Assert.Equal(1_350, account.BalanceCents);  // 1000 - 500 + 850
    }

    [Fact]
    public void SettleRound_InsufficientFunds_ReturnsError()
    {
        var account = NewAccount();
        account.Deposit(400, "dep", At());
        var result = account.SettleRound(Guid.NewGuid(), 500, 0, At());
        Assert.False(result.IsSuccess);
        Assert.Equal(WalletError.InsufficientFunds, result.Error);
        Assert.Equal(400, account.BalanceCents);  // unchanged
    }
}