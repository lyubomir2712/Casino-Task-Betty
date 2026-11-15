using System.Security.Cryptography;
using Games.Api.Domain;

namespace Games.Api.Infrastructure.Random;

public sealed class CryptoRandomSource : IRandomSource
{
    public double NextDouble()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);

        var value = BitConverter.ToUInt64(bytes) >> 11;
        return value * (1.0 / (1UL << 53));
    }
}