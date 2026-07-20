using System;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Implementación de seguridad utilizando el PasswordHasher de ASP.NET Core Identity.
/// Implementa PBKDF2 (Password-Based Key Derivation Function 2) con sal (salt) aleatoria
/// y factor de trabajo configurable por defecto de la plataforma.
/// Reemplaza la versión insegura basada en SHA-256 plano.
/// </summary>
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
        return result == PasswordVerificationResult.Success;
    }
}
