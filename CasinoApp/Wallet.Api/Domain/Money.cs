namespace CasinoApp.Domain;

public static class Money
{
    public static long ToCents(decimal amount) => (long)(amount * 100);

    public static decimal ToDollars(long cents) => cents / 100m;

    public static bool HasValidPrecision(decimal amount) => decimal.Round(amount, 2) == amount;
}