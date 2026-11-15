using Client;

var gatewayUrl = args.FirstOrDefault()
                 ?? Environment.GetEnvironmentVariable("CASINO_GATEWAY_URL")
                 ?? "https://localhost:7029/";

using var http = new HttpClient
{
    BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/"),
    Timeout = TimeSpan.FromSeconds(15)
};

var api = new CasinoApiClient(http);

Console.WriteLine("Welcome to the casino!");

var player = await LoginAsync(api);
if (player is null) return;

var session = new GameSession(api, player.PlayerId);

Console.WriteLine($"Welcome, {player.Name}! Your current balance is: {MoneyFormat.Format(player.Balance)}");
Console.WriteLine();
Console.WriteLine(GameSession.HelpText);

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input is null) break;   // input stream closed (e.g. Ctrl+D)

    var command = Command.Parse(input);

    if (command is Command.Exit)
    {
        Console.WriteLine("Thank you for playing! Hope to see you again soon.");
        break;
    }

    var output = command switch
    {
        Command.Deposit d  => await session.DepositAsync(d.Amount),
        Command.Withdraw w => await session.WithdrawAsync(w.Amount),
        Command.Bet b      => await session.BetAsync(b.Amount),
        Command.Balance    => await session.BalanceAsync(),
        Command.Invalid i  => i.Message,
        _                  => GameSession.HelpText
    };

    Console.WriteLine(output);
    Console.WriteLine();
}

static async Task<PlayerDto?> LoginAsync(CasinoApiClient api)
{
    while (true)
    {
        Console.Write("Enter your player name: ");
        var name = Console.ReadLine();

        if (name is null) return null;
        if (string.IsNullOrWhiteSpace(name)) continue;

        var result = await api.GetOrCreatePlayerAsync(name.Trim());
        if (result.IsSuccess) return result.Value;

        Console.WriteLine(result.Error);
    }
}