using System; // Importa ArgumentException, StringComparison, Convert
using System.Security.Cryptography; // Importa SHA256 (para soporte retrocompatible con hashes antiguos)
using System.Text; // Importa Encoding.UTF8
using ECommerce.Application.Interfaces; // Importa la interfaz IPasswordHasher
using Microsoft.AspNetCore.Identity; // Importa PasswordHasher<T> (algoritmo oficial de .NET Identity PBKDF2 con salt aleatorio)

namespace ECommerce.Infrastructure.Services; // Capa Infrastructure: implementación técnica de criptografía de contraseñas

public sealed class PasswordHasherService : IPasswordHasher
{
    // Instancia del hasheador oficial de ASP.NET Core Identity (aplica PBKDF2 con HMAC-SHA256/SHA512 y genera un Salt aleatorio por cada contraseña)
    private readonly PasswordHasher<object> _hasher = new();
    // Objeto comodín requerido por la firma de PasswordHasher<object>
    private readonly object _dummyUser = new();

    // Método que convierte una contraseña en texto plano ("AdminPassword123!") en un Hash irreversible para guardar en la BD
    public string Hash(string password)
    {
        // Valida que la contraseña no venga vacía ni con solo espacios
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        // Genera y devuelve el hash seguro (incluye el salt adentro del propio string resultante)
        return _hasher.HashPassword(_dummyUser, password);
    }

    // Método que verifica en el Login si la contraseña ingresada por el usuario coincide con el PasswordHash guardado en la tabla Users
    public bool Verify(string password, string passwordHash)
    {
        // Si el hash guardado estuviera vacío, devuelve false por seguridad
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        // Extrae el salt del passwordHash guardado, aplica el algoritmo a "password" y compara si dan el mismo resultado
        var result = _hasher.VerifyHashedPassword(_dummyUser, passwordHash, password);
        // Si el resultado es exitoso, devuelve true (contraseña correcta)
        if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            return true;

        // Soporte retrocompatible por si en la base de datos había algún usuario viejo guardado con hash SHA-256 hexadecimal de 64 caracteres
        if (LooksLikeLegacySha256Hash(passwordHash))
        {
            var legacyHash = ComputeSha256Hex(password);
            return string.Equals(passwordHash, legacyHash, StringComparison.OrdinalIgnoreCase);
        }

        // Si no coincidió, devuelve false (el Handler lanzará AuthenticationException -> 401 Unauthorized)
        return false;
    }

    // Verifica si un string tiene exactamente 64 caracteres hexadecimales (formato típico de SHA-256)
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

    // Calcula el hash SHA-256 en formato hexadecimal para comparar con hashes legados
    private static string ComputeSha256Hex(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
