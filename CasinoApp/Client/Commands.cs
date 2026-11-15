using System.Globalization;

namespace Client;

public abstract record Command
{
    public sealed record Deposit(decimal Amount) : Command;
    public sealed record Withdraw(decimal Amount) : Command;
    public sealed record Bet(decimal Amount) : Command;
    public sealed record Balance : Command;
    public sealed record Help : Command;
    public sealed record Exit : Command;
    public sealed record Invalid(string Message) : Command;

    public static Command Parse(string input)
    {
        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return new Invalid("Please enter a command. Type 'help' to see the options.");

        var name = parts[0].ToLowerInvariant();

        return name switch
        {
            "exit" or "quit"                  => new Exit(),
            "help"                            => new Help(),
            "balance"                         => new Balance(),
            "deposit" or "withdraw" or "bet"  => ParseAmountCommand(name, parts),
            _ => new Invalid($"Unknown command '{parts[0]}'. Type 'help' to see the options.")
        };
    }

    private static Command ParseAmountCommand(string name, string[] parts)
    {
        if (parts.Length != 2 ||
            !decimal.TryParse(parts[1].TrimStart('$'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ||
            amount <= 0)
        {
            return new Invalid($"Usage: {name} <amount>, e.g. '{name} 10' or '{name} 13.50'.");
        }

        return name switch
        {
            "deposit"  => new Deposit(amount),
            "withdraw" => new Withdraw(amount),
            _          => new Bet(amount)
        };
    }
}