namespace Casino.IntegrationTests;

[Collection(nameof(CasinoCollection))]
public class GatewayTests(CasinoAppFixture fixture)
{
    private HttpClient Gateway => fixture.Gateway;

    [Fact]
    public async Task InternalRoundsEndpoint_Returns404_ThroughGateway()
    {
        // The gateway only proxies /api/players/** and /api/bets/**.
        // /api/internal/** is not routed, so the gateway returns 404.
        var resp = await Gateway.PostAsJsonAsync(
            "api/internal/rounds",
            new { PlayerId = Guid.NewGuid(), RoundId = Guid.NewGuid(), BetCents = 500, WinCents = 0 });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}