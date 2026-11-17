using Casino.Client.Commands;

namespace Casino.Client.Tests;

public class CommandsTests
{
    // --- deposit ---

    [Theory]
    [InlineData("deposit 10", 10)]
    [InlineData("deposit 13.50", 13.50)]
    [InlineData("DEPOSIT 5", 5)]
    [InlineData("deposit $5", 5)]
    [InlineData("deposit $0.99", 0.99)]
    public void Parse_Deposit_ReturnsCorrectAmount(string input, decimal expected)
    {
        var cmd = Assert.IsType<Command.Deposit>(Command.Parse(input));
        Assert.Equal(expected, cmd.Amount);
    }

    // --- withdraw ---

    [Theory]
    [InlineData("withdraw 5.50", 5.50)]
    [InlineData("withdraw $20", 20)]
    [InlineData("WITHDRAW 3", 3)]
    public void Parse_Withdraw_ReturnsCorrectAmount(string input, decimal expected)
    {
        var cmd = Assert.IsType<Command.Withdraw>(Command.Parse(input));
        Assert.Equal(expected, cmd.Amount);
    }

    // --- bet ---

    [Theory]
    [InlineData("bet 1", 1)]
    [InlineData("bet 10", 10)]
    [InlineData("BET $5", 5)]
    public void Parse_Bet_ReturnsCorrectAmount(string input, decimal expected)
    {
        var cmd = Assert.IsType<Command.Bet>(Command.Parse(input));
        Assert.Equal(expected, cmd.Amount);
    }

    // --- balance / exit / help ---

    [Fact]
    public void Parse_Balance_ReturnsBalance() =>
        Assert.IsType<Command.Balance>(Command.Parse("balance"));

    [Theory]
    [InlineData("exit")]
    [InlineData("quit")]
    [InlineData("EXIT")]
    [InlineData("QUIT")]
    public void Parse_ExitOrQuit_ReturnsExit(string input) =>
        Assert.IsType<Command.Exit>(Command.Parse(input));

    [Fact]
    public void Parse_Help_ReturnsHelp() =>
        Assert.IsType<Command.Help>(Command.Parse("help"));

    // --- bad input ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("foo")]
    [InlineData("deposit")]          // missing amount
    [InlineData("deposit -5")]       // negative
    [InlineData("deposit 0")]        // zero
    [InlineData("deposit abc")]      // non-numeric
    [InlineData("deposit 1 2")]      // extra token
    public void Parse_BadInput_ReturnsInvalid(string input) =>
        Assert.IsType<Command.Invalid>(Command.Parse(input));
}