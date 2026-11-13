namespace Games.Api.Domain;

public interface IRandomSource
{
    /// <summary>Returns a value in [0, 1).</summary>
    double NextDouble();
}