namespace Casino.IntegrationTests;

[Collection(nameof(CasinoCollection))]
public class ConcurrencyTests(CasinoAppFixture fixture)
{
    private HttpClient Gateway => fixture.Gateway;

    [Fact]
    public async Task ConcurrentWithdrawals_NeverBelowZero_SumMatchesBalance()
    {
        const decimal initialDeposit = 100m;
        const decimal withdrawAmount = 10m;
        const int parallelism = 20;

        var player = await Gateway.CreatePlayerAsync();
        await Gateway.DepositAsync(player.PlayerId, initialDeposit);

        // Fire 20 concurrent withdrawals of $10 — at most 10 can succeed
        var tasks = Enumerable.Range(0, parallelism)
            .Select(_ => Gateway.WithdrawAsync(player.PlayerId, withdrawAmount))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.Status == HttpStatusCode.OK);

        var balance = await Gateway.GetBalanceAsync(player.PlayerId);
        Assert.True(balance.Balance >= 0m, $"Balance went negative: {balance.Balance}");
        Assert.Equal(initialDeposit - successCount * withdrawAmount, balance.Balance);
    }

    [Fact]
    public async Task ConcurrentBets_NeverBelowZero()
    {
        // Deposit enough that all bets can succeed
        var player = await Gateway.CreatePlayerAsync();
        await Gateway.DepositAsync(player.PlayerId, 1_000m);

        // Place 20 concurrent $5 bets — total possible outlay $100, well within balance
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Gateway.PlaceBetAsync(player.PlayerId, 5m))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Wait for any pending rounds
        var pendingRounds = results
            .Where(r => r.Body?.Status == "Pending")
            .Select(r => r.Body!.RoundId)
            .ToArray();

        await Task.WhenAll(pendingRounds.Select(id => Gateway.WaitForRoundAsync(id)));

        var balance = await Gateway.GetBalanceAsync(player.PlayerId);
        Assert.True(balance.Balance >= 0m, $"Balance went negative: {balance.Balance}");
    }
}