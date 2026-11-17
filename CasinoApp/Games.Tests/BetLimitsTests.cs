using Games.Api.Domain.Rules;

namespace Games.Tests;

public class BetLimitsTests
{
    [Theory]
    [InlineData(100)]    // $1.00 — minimum
    [InlineData(1_000)]  // $10.00 — maximum
    public void ValidBets_Pass(long cents) => Assert.True(BetLimits.IsValid(cents));

    [Theory]
    [InlineData(99)]     // $0.99
    [InlineData(1_001)]  // $10.01
    [InlineData(0)]
    public void InvalidBets_Fail(long cents) => Assert.False(BetLimits.IsValid(cents));
}