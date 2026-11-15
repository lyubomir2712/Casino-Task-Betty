using System.Globalization;

namespace Client;

public static class MoneyFormat
{
    public static string Format(decimal amount) =>
        "$" + (amount % 1 == 0
            ? amount.ToString("0", CultureInfo.InvariantCulture)
            : amount.ToString("0.00", CultureInfo.InvariantCulture));
}