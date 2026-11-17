namespace Casino.Client.Tests;

public class MoneyFormatTests
{
    [Theory]
    [InlineData(10, "$10")]
    [InlineData(0, "$0")]
    [InlineData(13.50, "$13.50")]
    [InlineData(0.01, "$0.01")]
    [InlineData(100, "$100")]
    public void Format_ReturnsExpectedString(decimal amount, string expected) =>
        Assert.Equal(expected, MoneyFormat.Format(amount));
}