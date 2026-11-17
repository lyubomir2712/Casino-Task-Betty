namespace Casino.IntegrationTests;

[Collection(nameof(CasinoCollection))]
public class IdempotencyTests(CasinoAppFixture fixture)
{
    private HttpClient Gateway => fixture.Gateway;

    [Fact]
    public async Task SameDepositKey_Twice_MovesMoneyOnce()
    {
        var player = await Gateway.CreatePlayerAsync();
        var key = Guid.NewGuid().ToString();

        // First deposit
        var first = await Gateway.DepositAsync(player.PlayerId, 50m, idempotencyKey: key);
        Assert.Equal(50m, first.Balance);

        // Second call with the same key — idempotent, same result
        var second = await Gateway.DepositAsync(player.PlayerId, 50m, idempotencyKey: key);
        Assert.Equal(50m, second.Balance);  // balance did NOT double

        var balance = await Gateway.GetBalanceAsync(player.PlayerId);
        Assert.Equal(50m, balance.Balance);
    }

    [Fact]
    public async Task SameRoundId_SettledTwiceDirectly_MovesMoneyOnce()
    {
        // Create player and deposit
        var player = await Gateway.CreatePlayerAsync();
        await Gateway.DepositAsync(player.PlayerId, 100m);

        // Place a bet and wait for it to settle
        var (bet, _) = await Gateway.PlaceBetAsync(player.PlayerId, 5m);
        Assert.NotNull(bet);
        var round = bet!.Status == "Pending"
            ? await Gateway.WaitForRoundAsync(bet.RoundId)
            : bet;
        Assert.Equal("Settled", round.Status);

        var balanceAfterRound = await Gateway.GetBalanceAsync(player.PlayerId);

        // Now call /internal/rounds directly on Wallet-Api with the same roundId
        using var walletClient = fixture.App.CreateHttpClient("Wallet-Api");
        walletClient.Timeout = TimeSpan.FromSeconds(15);

        var betCents = (long)(round.Bet * 100);
        var winCents = (long)(round.Win * 100);
        var settleReq = new SettleRoundRequest(player.PlayerId, round.RoundId, betCents, winCents);

        var resp = await walletClient.PostAsJsonAsync("/internal/rounds", settleReq);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var settle = await resp.Content.ReadFromJsonAsync<SettleRoundResponse>();

        // Balance must be unchanged — idempotent second settlement
        Assert.Equal((long)(balanceAfterRound.Balance * 100), settle!.BalanceCents);

        var finalBalance = await Gateway.GetBalanceAsync(player.PlayerId);
        Assert.Equal(balanceAfterRound.Balance, finalBalance.Balance);
    }
}