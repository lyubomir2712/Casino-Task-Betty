using Games.Api.Domain.Rules;

namespace Games.Tests;

public class OutcomeCalculatorTests
{
    private static OutcomeCalculator Calc(params double[] rolls) =>
        new(new FixedRandomSource(rolls));

    // --- band boundaries ---

    [Fact]
    public void Roll_0_49_IsLoss()
    {
        Assert.Equal(0, Calc(0.49).CalculateWinCents(500));
    }

    [Fact]
    public void Roll_0_50_IsSmallWin()
    {
        // roll=0.50 → not a loss; secondRoll=0.5 → r=0.5 → multiplier=1.0 → win=500
        var win = Calc(0.50, 0.5).CalculateWinCents(500);
        Assert.True(win > 0 && win <= 2 * 500, $"Expected small win (0,1000], got {win}");
    }

    [Fact]
    public void Roll_0_90_IsBigWin()
    {
        // roll=0.90 → big win; secondRoll=0.5 → r=0.5 → multiplier=6.0 → win=3000
        var win = Calc(0.90, 0.5).CalculateWinCents(500);
        Assert.True(win > 2 * 500, $"Expected big win >1000, got {win}");
    }

    // --- big win invariant ---

    [Fact]
    public void BigWin_AlwaysMoreThanTwoBet()
    {
        for (var i = 0; i <= 90; i++)
        {
            var secondRoll = i * 0.01;
            var win = Calc(0.90, secondRoll).CalculateWinCents(500);
            Assert.True(win > 2 * 500, $"secondRoll={secondRoll}: win={win} not > 1000");
        }
    }

    // --- minimum win ---

    [Fact]
    public void MinimumWin_IsOneCent()
    {
        Assert.Equal(1, Calc(0.50, 0.9999999).CalculateWinCents(1));
    }

    // --- brief example ---

    [Fact]
    public void BriefExample_FiveDollarBetReturnsEightFifty()
    {
        Assert.Equal(850, Calc(0.50, 0.15).CalculateWinCents(500));
    }

    // --- statistical distribution ---

    [Fact]
    public void StatisticalDistribution_BandsLandNear50_40_10()
    {
        var calc = new OutcomeCalculator(new SystemRandomSource());
        const int iterations = 100_000;
        const long betCents = 500;

        int losses = 0, smallWins = 0, bigWins = 0;
        for (var i = 0; i < iterations; i++)
        {
            var win = calc.CalculateWinCents(betCents);
            if (win == 0) losses++;
            else if (win <= 2 * betCents) smallWins++;
            else bigWins++;
        }

        Assert.InRange((double)losses / iterations, 0.47, 0.53);
        Assert.InRange((double)smallWins / iterations, 0.37, 0.43);
        Assert.InRange((double)bigWins / iterations, 0.07, 0.13);
    }
}