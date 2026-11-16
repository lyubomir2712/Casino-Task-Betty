using Games.Api.Domain.Interfaces;

namespace Games.Api.Domain.Rules;

public sealed class OutcomeCalculator(IRandomSource random)
{
    public const double LoseProbability = 0.50;
    public const double SmallWinProbability = 0.40;

    public long CalculateWinCents(long betCents)
    {
        var roll = random.NextDouble();

        if (roll < LoseProbability)
            return 0;
        
        var r = 1 - random.NextDouble();

        var multiplier = roll < LoseProbability + SmallWinProbability
            ? 2 * r
            : 2 + 8 * r;

        var winCents = (long)Math.Round(betCents * multiplier, MidpointRounding.AwayFromZero);
        return Math.Max(1, winCents);
    }
}