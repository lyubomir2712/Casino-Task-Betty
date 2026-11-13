namespace Games.Api.Domain;

public sealed class Round
{
    private Round() { }

    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public long BetCents { get; private set; }
    public long WinCents { get; private set; }
    public RoundStatus Status { get; private set; }
    public long? BalanceAfterCents { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static Result<Round> Create(
        Guid playerId, long betCents, OutcomeCalculator calculator, DateTimeOffset now)
    {
        if (!BetLimits.IsValid(betCents)) return GameError.InvalidBet;

        return new Round
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            BetCents = betCents,
            WinCents = calculator.CalculateWinCents(betCents),
            Status = RoundStatus.Pending,
            CreatedAt = now
        };
    }

    public Result<Round> MarkSettled(long balanceAfterCents, DateTimeOffset now)
    {
        if (Status != RoundStatus.Pending) return GameError.InvalidStateTransition;

        Status = RoundStatus.Settled;
        BalanceAfterCents = balanceAfterCents;
        CompletedAt = now;
        return this;
    }

    public Result<Round> MarkRejected(DateTimeOffset now)
    {
        if (Status != RoundStatus.Pending) return GameError.InvalidStateTransition;

        Status = RoundStatus.Rejected;
        CompletedAt = now;
        return this;
    }
}