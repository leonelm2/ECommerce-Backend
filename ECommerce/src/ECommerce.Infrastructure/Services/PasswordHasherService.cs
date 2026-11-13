using System;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _dummyUser = new();

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        return _hasher.HashPassword(_dummyUser, password);
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var result = _hasher.VerifyHashedPassword(_dummyUser, passwordHash, password);
        if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            return true;

        if (LooksLikeLegacySha256Hash(passwordHash))
        {
            var legacyHash = ComputeSha256Hex(password);
            return string.Equals(passwordHash, legacyHash, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool LooksLikeLegacySha256Hash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length != 64)
            return false;

        foreach (var ch in passwordHash)
        {
            var isHexDigit = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
            if (!isHexDigit)
                return false;
        }

        return true;
    }

    private static string ComputeSha256Hex(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
