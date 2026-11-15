using System.Security.Cryptography;
using System.Text;

namespace Core.Security;

/// <summary>
/// Legacy MD5 encoding retained only for verifying existing database hashes.
/// New passwords must be created with UserPasswordHasher.
/// </summary>
public static class PasswordEncript
{
    public static string EncriptPassword(string pass)
    {
        var originalBytes = Encoding.UTF8.GetBytes(pass);
        var encodedBytes = MD5.HashData(originalBytes);
        return BitConverter.ToString(encodedBytes);
    }
}
