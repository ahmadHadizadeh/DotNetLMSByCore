using System.Security.Cryptography;
using System.Text;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Core.Security;

/// <summary>
/// Hashes new account passwords with ASP.NET Core Identity V3 and verifies
/// the legacy MD5 format only to support a one-time migration at login.
/// </summary>
public sealed class UserPasswordHasher
{
    public const int CurrentIterationCount = 220_000;

    private readonly PasswordHasher<Users> _identityHasher;

    public UserPasswordHasher()
    {
        _identityHasher = new PasswordHasher<Users>(
            Options.Create(new PasswordHasherOptions
            {
                CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                IterationCount = CurrentIterationCount
            }));
    }

    public string HashPassword(Users user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(password);

        return _identityHasher.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyPassword(
        Users user,
        string storedHash,
        string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(storedHash);
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (IsLegacyMd5Hash(storedHash))
        {
            var expectedHash = Encoding.ASCII.GetBytes(
                PasswordEncript.EncriptPassword(providedPassword));
            var actualHash = Encoding.ASCII.GetBytes(storedHash);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }

        if (!HasSupportedIdentityFormatMarker(storedHash))
        {
            return PasswordVerificationResult.Failed;
        }

        return _identityHasher.VerifyHashedPassword(user, storedHash, providedPassword);
    }

    private static bool IsLegacyMd5Hash(string storedHash)
    {
        if (storedHash.Length != 47)
        {
            return false;
        }

        for (var index = 0; index < storedHash.Length; index++)
        {
            var character = storedHash[index];

            if (index % 3 == 2)
            {
                if (character != '-')
                {
                    return false;
                }

                continue;
            }

            var isHexCharacter =
                character is >= '0' and <= '9'
                or >= 'A' and <= 'F'
                or >= 'a' and <= 'f';

            if (!isHexCharacter)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasSupportedIdentityFormatMarker(string storedHash)
    {
        byte[] decodedHash;

        try
        {
            decodedHash = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        return decodedHash.Length > 0 && decodedHash[0] is 0x00 or 0x01;
    }
}
