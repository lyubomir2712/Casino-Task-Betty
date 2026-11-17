using Games.Api.Domain.Interfaces;

namespace Games.Tests;

internal sealed class FixedRandomSource(params double[] values) : IRandomSource
{
    private int _index;
    public double NextDouble() => values[_index++ % values.Length];
}

internal sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _rng = new();
    public double NextDouble() => _rng.NextDouble();
}