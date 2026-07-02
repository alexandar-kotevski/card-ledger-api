using System.Security.Cryptography;
using System.Text;

namespace CardLedger.Domain.Services;

public static class CvvHasher
{
    public static string Hash(string cvv)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(cvv));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string cvv, string hash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(cvv)),
            Encoding.UTF8.GetBytes(hash));
}
