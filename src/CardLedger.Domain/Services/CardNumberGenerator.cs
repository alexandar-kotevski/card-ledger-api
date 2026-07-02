using System.Security.Cryptography;

namespace CardLedger.Domain.Services;

public static class CardNumberGenerator
{
    public static string Generate()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt64(bytes) % 10_000_000_000_000_000UL;
        return value.ToString("D16");
    }
}
