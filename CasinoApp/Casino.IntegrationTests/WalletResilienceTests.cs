namespace Casino.IntegrationTests;

// Runs with its own Aspire instance so stopping/starting Wallet-Api doesn't affect the shared collection.
public class WalletResilienceTests : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private HttpClient _gateway = null!;
    private ResourceNotificationService _notifications = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Casino_AppHost>();
        _app = await builder.BuildAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _app.StartAsync(cts.Token);

        _notifications = _app.Services.GetRequiredService<ResourceNotificationService>();
        await _notifications.WaitForResourceAsync("gateway", KnownResourceStates.Running, cts.Token);

        _gateway = _app.CreateHttpClient("gateway");
        _gateway.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task DisposeAsync()
    {
        _gateway.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task WalletDown_BetReturnsPending_WorkerSettlesExactlyOnce()
    {
        // 1. Create player and deposit while everything is healthy
        var player = await _gateway.CreatePlayerAsync();
        await _gateway.DepositAsync(player.PlayerId, 100m);

        // 2. Stop Wallet-Api
        var commandService = _app.Services.GetRequiredService<ResourceCommandService>();
        await commandService.ExecuteCommandAsync("Wallet-Api", KnownResourceCommands.StopCommand, CancellationToken.None);
        await _notifications.WaitForResourceAsync("Wallet-Api", KnownResourceStates.Stopping,
            new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);

        // 3. Place a bet — Games.Api cannot reach Wallet, so it returns 202 Pending
        var (bet, betStatus) = await _gateway.PlaceBetAsync(player.PlayerId, 5m);
        Assert.NotNull(bet);
        Assert.Equal(HttpStatusCode.Accepted, betStatus);
        Assert.Equal("Pending", bet!.Status);

        // 4. Bring Wallet-Api back up
        await commandService.ExecuteCommandAsync("Wallet-Api", KnownResourceCommands.StartCommand, CancellationToken.None);
        await _notifications.WaitForResourceAsync("Wallet-Api", KnownResourceStates.Running,
            new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);

        // 5. The PendingRoundsWorker will pick up the round within ~15 seconds. Wait up to 60 s.
        var settled = await _gateway.WaitForRoundAsync(bet.RoundId, TimeSpan.FromSeconds(60));
        Assert.True(settled.Status is "Settled" or "Rejected",
            $"Round still {settled.Status} after timeout");

        // 6. Verify money moved exactly once
        var balance = await _gateway.GetBalanceAsync(player.PlayerId);
        if (settled.Status == "Settled")
            Assert.Equal(100m - settled.Bet + settled.Win, balance.Balance);
        else
            Assert.Equal(100m, balance.Balance);

        // 7. Second settlement call must be idempotent — balance unchanged
        using var walletClient = _app.CreateHttpClient("Wallet-Api");
        var settleReq = new SettleRoundRequest(
            player.PlayerId, bet.RoundId,
            (long)(settled.Bet * 100), (long)(settled.Win * 100));
        var secondSettle = await walletClient.PostAsJsonAsync("/internal/rounds", settleReq);
        Assert.Equal(HttpStatusCode.OK, secondSettle.StatusCode);

        var balanceAfterRetry = await _gateway.GetBalanceAsync(player.PlayerId);
        Assert.Equal(balance.Balance, balanceAfterRetry.Balance);
    }
}
