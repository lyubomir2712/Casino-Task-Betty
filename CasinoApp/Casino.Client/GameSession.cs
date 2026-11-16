using Client;
using Client.Dtos;

namespace Casino.Client;

public sealed class GameSession(CasinoApiClient api, Guid playerId)
{
    private const int MaxPolls = 5;
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(1);

    public const string HelpText =
        """
        Commands:
          deposit <amount>   add funds, e.g. deposit 10
          withdraw <amount>  withdraw funds, e.g. withdraw 13.50
          bet <amount>       play a round ($1 to $10), e.g. bet 5
          balance            show your balance
          exit               leave the casino
        """;

    public async Task<string> DepositAsync(decimal amount, CancellationToken ct = default)
    {
        var result = await api.DepositAsync(playerId, amount, ct);
        return result.IsSuccess
            ? $"Your deposit of {MoneyFormat.Format(result.Value!.Amount)} was successful. Your current balance is: {MoneyFormat.Format(result.Value.Balance)}"
            : result.Error!;
    }

    public async Task<string> WithdrawAsync(decimal amount, CancellationToken ct = default)
    {
        var result = await api.WithdrawAsync(playerId, amount, ct);

        if (result.IsSuccess)
            return $"Your withdrawal of {MoneyFormat.Format(result.Value!.Amount)} was successful. Your current balance is: {MoneyFormat.Format(result.Value.Balance)}";

        return result.StatusCode == 422 ? await WithBalanceAsync(result.Error!, ct) : result.Error!;
    }

    public async Task<string> BetAsync(decimal amount, CancellationToken ct = default)
    {
        var result = await api.PlaceBetAsync(playerId, amount, ct);

        if (!result.IsSuccess)
            return result.StatusCode == 422 ? await WithBalanceAsync(result.Error!, ct) : result.Error!;

        var round = result.Value!;
        if (round.Status == "Pending")
            round = await WaitForRoundAsync(round.RoundId, ct) ?? round;

        return round.Status switch
        {
            "Settled" when round.Win > 0 =>
                $"Congrats - you won {MoneyFormat.Format(round.Win)}! Your current balance is: {MoneyFormat.Format(round.Balance!.Value)}",
            "Settled" =>
                $"No luck this time! Your current balance is: {MoneyFormat.Format(round.Balance!.Value)}",
            "Rejected" =>
                await WithBalanceAsync("Your bet was rejected.", ct),
            _ =>
                "Your bet is still being processed. Type 'balance' in a moment to see the result."
        };
    }

    public async Task<string> BalanceAsync(CancellationToken ct = default)
    {
        var result = await api.GetBalanceAsync(playerId, ct);
        return result.IsSuccess
            ? $"Your current balance is: {MoneyFormat.Format(result.Value!.Balance)}"
            : result.Error!;
    }

    private async Task<RoundDto?> WaitForRoundAsync(Guid roundId, CancellationToken ct)
    {
        for (var i = 0; i < MaxPolls; i++)
        {
            await Task.Delay(PollDelay, ct);

            var result = await api.GetRoundAsync(roundId, ct);
            if (result.IsSuccess && result.Value!.Status != "Pending")
                return result.Value;
        }

        return null;
    }

    private async Task<string> WithBalanceAsync(string message, CancellationToken ct)
    {
        var balance = await api.GetBalanceAsync(playerId, ct);
        return balance.IsSuccess
            ? $"{message} Your current balance is: {MoneyFormat.Format(balance.Value!.Balance)}"
            : message;
    }
}