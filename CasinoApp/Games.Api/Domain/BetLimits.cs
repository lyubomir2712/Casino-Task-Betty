namespace Games.Api.Domain;

public static class BetLimits
{
    public const long MinCents = 100;
    public const long MaxCents = 1_000;
    
    public static bool IsValid(long betCents) => betCents is >= MinCents and <= MaxCents;
}