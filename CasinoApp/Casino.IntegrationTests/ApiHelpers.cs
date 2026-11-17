namespace Casino.IntegrationTests;

// --- request / response DTOs ---

record CreatePlayerRequest(string Name);
record CreatePlayerResponse(Guid PlayerId, string Name, decimal Balance);
record DepositRequest(decimal Amount);
record MoneyResponse(decimal Amount, decimal Balance);
record BalanceResponse(Guid PlayerId, decimal Balance);
record PlaceBetRequest(Guid PlayerId, decimal Amount);
record BetResponse(Guid RoundId, string Status, decimal Bet, decimal Win, decimal? Balance);
record SettleRoundRequest(Guid PlayerId, Guid RoundId, long BetCents, long WinCents);
record SettleRoundResponse(Guid RoundId, long BalanceCents);

// --- helper extensions ---

static class ApiHelpers
{
    public static async Task<CreatePlayerResponse> CreatePlayerAsync(
        this HttpClient client, string? name = null)
    {
        name ??= $"player_{Guid.NewGuid():N}";
        var resp = await client.PostAsJsonAsync("api/players", new CreatePlayerRequest(name));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreatePlayerResponse>())!;
    }

    public static async Task<MoneyResponse> DepositAsync(
        this HttpClient client, Guid playerId, decimal amount, string? idempotencyKey = null)
    {
        idempotencyKey ??= Guid.NewGuid().ToString();
        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"api/players/{playerId}/deposit")
        {
            Content = JsonContent.Create(new DepositRequest(amount))
        };
        req.Headers.Add("Idempotency-Key", idempotencyKey);
        var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MoneyResponse>())!;
    }

    public static async Task<(MoneyResponse? Body, HttpStatusCode Status)> WithdrawAsync(
        this HttpClient client, Guid playerId, decimal amount, string? idempotencyKey = null)
    {
        idempotencyKey ??= Guid.NewGuid().ToString();
        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"api/players/{playerId}/withdraw")
        {
            Content = JsonContent.Create(new DepositRequest(amount))
        };
        req.Headers.Add("Idempotency-Key", idempotencyKey);
        var resp = await client.SendAsync(req);
        MoneyResponse? body = null;
        if (resp.IsSuccessStatusCode)
            body = await resp.Content.ReadFromJsonAsync<MoneyResponse>();
        return (body, resp.StatusCode);
    }

    public static async Task<(BetResponse? Body, HttpStatusCode Status)> PlaceBetAsync(
        this HttpClient client, Guid playerId, decimal amount)
    {
        var resp = await client.PostAsJsonAsync("api/bets", new PlaceBetRequest(playerId, amount));
        BetResponse? body = null;
        if (resp.IsSuccessStatusCode)
            body = await resp.Content.ReadFromJsonAsync<BetResponse>();
        return (body, resp.StatusCode);
    }

    public static async Task<BetResponse> GetRoundAsync(this HttpClient client, Guid roundId)
    {
        var resp = await client.GetAsync($"api/bets/{roundId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<BetResponse>())!;
    }

    public static async Task<BalanceResponse> GetBalanceAsync(this HttpClient client, Guid playerId)
    {
        var resp = await client.GetAsync($"api/players/{playerId}/balance");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<BalanceResponse>())!;
    }

    public static async Task<BetResponse> WaitForRoundAsync(
        this HttpClient client, Guid roundId, TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        while (true)
        {
            var round = await client.GetRoundAsync(roundId);
            if (round.Status != "Pending") return round;
            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException($"Round {roundId} still Pending after timeout.");
            await Task.Delay(2_000);
        }
    }
}