using System.Net.Http.Json;
using System.Text.Json;
using Client.Dtos;

namespace Casino.Client;

public sealed class CasinoApiClient(HttpClient http)
{
    private const int MaxAttempts = 3;
    private const string Unreachable = "Can't reach the casino right now. Please try again in a moment.";

    private sealed record ProblemDto(string? Title, string? Detail, Dictionary<string, string[]>? Errors);

    public Task<ApiResult<PlayerDto>> GetOrCreatePlayerAsync(string name, CancellationToken ct = default) =>
        SendAsync<PlayerDto>(() => Post("api/players", new { name }), retry: true, ct);

    public Task<ApiResult<MoneyDto>> DepositAsync(Guid playerId, decimal amount, CancellationToken ct = default)
    {
        var key = Guid.NewGuid().ToString();
        return SendAsync<MoneyDto>(() => Post($"api/players/{playerId}/deposit", new { amount }, key), retry: true, ct);
    }

    public Task<ApiResult<MoneyDto>> WithdrawAsync(Guid playerId, decimal amount, CancellationToken ct = default)
    {
        var key = Guid.NewGuid().ToString();
        return SendAsync<MoneyDto>(() => Post($"api/players/{playerId}/withdraw", new { amount }, key), retry: true, ct);
    }

    public Task<ApiResult<BalanceDto>> GetBalanceAsync(Guid playerId, CancellationToken ct = default) =>
        SendAsync<BalanceDto>(() => new HttpRequestMessage(HttpMethod.Get, $"api/players/{playerId}/balance"), retry: true, ct);

    public Task<ApiResult<RoundDto>> PlaceBetAsync(Guid playerId, decimal amount, CancellationToken ct = default) =>
        SendAsync<RoundDto>(() => Post("api/bets", new { playerId, amount }), retry: false, ct);

    public Task<ApiResult<RoundDto>> GetRoundAsync(Guid roundId, CancellationToken ct = default) =>
        SendAsync<RoundDto>(() => new HttpRequestMessage(HttpMethod.Get, $"api/bets/{roundId}"), retry: true, ct);

    private static HttpRequestMessage Post(string path, object body, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private async Task<ApiResult<T>> SendAsync<T>(Func<HttpRequestMessage> createRequest, bool retry, CancellationToken ct)
    {
        var maxAttempts = retry ? MaxAttempts : 1;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var request = createRequest();
                using var response = await http.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    var value = await response.Content.ReadFromJsonAsync<T>(ct);
                    return new ApiResult<T>(value, (int)response.StatusCode, null);
                }

                if ((int)response.StatusCode >= 500 && attempt < maxAttempts)
                {
                    await Task.Delay(Backoff(attempt), ct);
                    continue;
                }

                return new ApiResult<T>(default, (int)response.StatusCode, await ReadErrorAsync(response, ct));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                if (attempt >= maxAttempts)
                    return new ApiResult<T>(default, 0, Unreachable);

                await Task.Delay(Backoff(attempt), ct);
            }
        }
    }

    private static TimeSpan Backoff(int attempt) => TimeSpan.FromMilliseconds(300 * attempt);

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(ct);
            return problem?.Errors?.Values.SelectMany(v => v).FirstOrDefault()
                   ?? problem?.Detail
                   ?? problem?.Title
                   ?? $"Request failed ({(int)response.StatusCode}).";
        }
        catch (JsonException)
        {
            return $"Request failed ({(int)response.StatusCode}).";
        }
    }
}