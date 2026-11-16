using System.Net;
using Games.Api.Domain;
using Games.Api.Domain.Entities;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Games.Api.Infrastructure.Wallet;

public enum SettleStatus
{
    Settled,     
    Rejected,
    Unavailable
}

public sealed record SettleResult(SettleStatus Status, long BalanceCents = 0);

public sealed class WalletClient(HttpClient http, ILogger<WalletClient> logger)
{
    private sealed record SettleRoundRequest(Guid PlayerId, Guid RoundId, long BetCents, long WinCents);
    private sealed record SettleRoundResponse(Guid RoundId, long BalanceCents);

    public async Task<SettleResult> SettleRoundAsync(Round round, CancellationToken ct)
    {
        var request = new SettleRoundRequest(round.PlayerId, round.Id, round.BetCents, round.WinCents);

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("/internal/rounds", request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException
                                       or BrokenCircuitException or TaskCanceledException
                                   && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Wallet unavailable while settling round {RoundId}", round.Id);
            return new SettleResult(SettleStatus.Unavailable);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<SettleRoundResponse>(ct);
                return new SettleResult(SettleStatus.Settled, body!.BalanceCents);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.UnprocessableEntity or HttpStatusCode.NotFound or HttpStatusCode.BadRequest
                    => new SettleResult(SettleStatus.Rejected),
                _   => new SettleResult(SettleStatus.Unavailable)
            };
        }
    }
}