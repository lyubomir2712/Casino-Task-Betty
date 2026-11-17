using Games.Api.Domain.Entities;
using Games.Api.Domain.Enums;
using Games.Api.Domain.Errors;
using Games.Api.Domain.Rules;
using Microsoft.Extensions.Time.Testing;

namespace Games.Tests;

public class RoundTests
{
    private static readonly Guid PlayerId = Guid.NewGuid();

    // Calculator that always loses — used for state-transition tests where the win amount doesn't matter
    private static readonly OutcomeCalculator LoseCalculator = new(new FixedRandomSource(0.49));

    private static DateTimeOffset At(int year = 2024, int month = 1, int day = 1) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    // --- creation ---

    [Fact]
    public void Create_ValidBet_SetsPending()
    {
        var result = Round.Create(PlayerId, 500, LoseCalculator, At());
        Assert.True(result.IsSuccess);
        Assert.Equal(RoundStatus.Pending, result.Value!.Status);
    }

    [Fact]
    public void Create_ValidBet_OutcomeAlreadyDecided()
    {
        // $5 → $8.50 example: outcome is baked in at creation, before settlement
        var calc = new OutcomeCalculator(new FixedRandomSource(0.50, 0.15));
        var result = Round.Create(PlayerId, 500, calc, At());
        Assert.True(result.IsSuccess);
        Assert.Equal(850, result.Value!.WinCents);
    }

    [Fact]
    public void Create_UsesProvidedTimestamp()
    {
        var fake = new FakeTimeProvider(At(2024, 6, 15));
        var now = fake.GetUtcNow();
        var result = Round.Create(PlayerId, 500, LoseCalculator, now);
        Assert.Equal(now, result.Value!.CreatedAt);
    }

    [Theory]
    [InlineData(99)]     // $0.99
    [InlineData(1_001)]  // $10.01
    [InlineData(0)]
    public void Create_InvalidBet_ReturnsError(long betCents)
    {
        var result = Round.Create(PlayerId, betCents, LoseCalculator, At());
        Assert.False(result.IsSuccess);
        Assert.Equal(GameError.InvalidBet, result.Error);
    }

    // --- valid transitions ---

    [Fact]
    public void MarkSettled_FromPending_Succeeds()
    {
        var round = Round.Create(PlayerId, 500, LoseCalculator, At()).Value!;
        var result = round.MarkSettled(10_000, At().AddMinutes(1));
        Assert.True(result.IsSuccess);
        Assert.Equal(RoundStatus.Settled, round.Status);
        Assert.Equal(10_000, round.BalanceAfterCents);
    }

    [Fact]
    public void MarkRejected_FromPending_Succeeds()
    {
        var round = Round.Create(PlayerId, 500, LoseCalculator, At()).Value!;
        var result = round.MarkRejected(At().AddMinutes(1));
        Assert.True(result.IsSuccess);
        Assert.Equal(RoundStatus.Rejected, round.Status);
    }

    // --- invalid transitions ---

    [Fact]
    public void MarkSettled_FromSettled_ReturnsError()
    {
        var round = Round.Create(PlayerId, 500, LoseCalculator, At()).Value!;
        round.MarkSettled(10_000, At().AddMinutes(1));
        var result = round.MarkSettled(10_000, At().AddMinutes(2));
        Assert.False(result.IsSuccess);
        Assert.Equal(GameError.InvalidStateTransition, result.Error);
    }

    [Fact]
    public void MarkSettled_FromRejected_ReturnsError()
    {
        var round = Round.Create(PlayerId, 500, LoseCalculator, At()).Value!;
        round.MarkRejected(At().AddMinutes(1));
        var result = round.MarkSettled(10_000, At().AddMinutes(2));
        Assert.False(result.IsSuccess);
        Assert.Equal(GameError.InvalidStateTransition, result.Error);
    }

    [Fact]
    public void MarkRejected_FromSettled_ReturnsError()
    {
        var round = Round.Create(PlayerId, 500, LoseCalculator, At()).Value!;
        round.MarkSettled(10_000, At().AddMinutes(1));
        var result = round.MarkRejected(At().AddMinutes(2));
        Assert.False(result.IsSuccess);
        Assert.Equal(GameError.InvalidStateTransition, result.Error);
    }
}