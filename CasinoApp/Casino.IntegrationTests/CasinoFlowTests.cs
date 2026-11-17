namespace Casino.IntegrationTests;

[Collection(nameof(CasinoCollection))]
public class CasinoFlowTests(CasinoAppFixture fixture)
{
    private HttpClient Gateway => fixture.Gateway;

    [Fact]
    public async Task FullFlow_CreateDepositBetWithdraw_BalanceConsistent()
    {
        // 1. Create player
        var player = await Gateway.CreatePlayerAsync();
        Assert.Equal(0m, player.Balance);

        // 2. Deposit $100
        var deposit = await Gateway.DepositAsync(player.PlayerId, 100m);
        Assert.Equal(100m, deposit.Balance);

        // 3. Confirm balance
        var balanceAfterDeposit = await Gateway.GetBalanceAsync(player.PlayerId);
        Assert.Equal(100m, balanceAfterDeposit.Balance);

        // 4. Place $5 bet (may return 200 Settled or 202 Pending)
        var (bet, betStatus) = await Gateway.PlaceBetAsync(player.PlayerId, 5m);
        Assert.NotNull(bet);
        Assert.True(betStatus is HttpStatusCode.OK or HttpStatusCode.Accepted);

        // 5. Wait for the round to settle if still pending
        var round = bet!.Status == "Pending"
            ? await Gateway.WaitForRoundAsync(bet.RoundId)
            : bet;

        Assert.True(round.Status is "Settled" or "Rejected");

        // 6. Verify balance reflects the round outcome
        var balanceAfterBet = await Gateway.GetBalanceAsync(player.PlayerId);
        if (round.Status == "Settled")
        {
            var expectedBalance = 100m - round.Bet + round.Win;
            Assert.Equal(expectedBalance, balanceAfterBet.Balance);
        }
        else // Rejected (insufficient funds edge case)
        {
            Assert.Equal(100m, balanceAfterBet.Balance);
        }

        // 7. Withdraw everything
        var remaining = balanceAfterBet.Balance;
        if (remaining > 0)
        {
            var (withdrawal, _) = await Gateway.WithdrawAsync(player.PlayerId, remaining);
            Assert.NotNull(withdrawal);
            Assert.Equal(0m, withdrawal!.Balance);
        }

        // 8. Confirm empty balance
        var finalBalance = await Gateway.GetBalanceAsync(player.PlayerId);
        Assert.Equal(0m, finalBalance.Balance);
    }
}