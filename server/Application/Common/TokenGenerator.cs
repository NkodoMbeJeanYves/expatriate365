namespace server.Application.Common;

public static class TokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure token.
    /// Returns (plainToken, hashHex) — transmit plain, store hash.
    /// </summary>
    public static (string Plain, string Hash) Generate(int byteCount = 32)
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(byteCount);
        var plain = Convert.ToHexString(bytes).ToLowerInvariant();
        var hash  = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(bytes)
        ).ToLowerInvariant();
        return (plain, hash);
    }

    /// <summary>
    /// Recomputes the SHA-256 hash of a plain hex token received from the client.
    /// Returns null if the input is not valid hex.
    /// </summary>
    public static string? Hash(string plainHex)
    {
        try
        {
            var bytes = Convert.FromHexString(plainHex);
            return Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(bytes)
            ).ToLowerInvariant();
        }
        catch { return null; }
    }
}
