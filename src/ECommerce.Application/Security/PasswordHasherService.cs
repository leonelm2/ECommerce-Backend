using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Interfaces;

namespace ECommerce.Application.Security;

/// <summary>
/// Implementación inyectable de IPasswordHasher.
/// Convierte la clase estática PasswordHasher en un servicio registrable en el contenedor DI.
/// La clase PasswordHasher estática se conserva para el seeding en Program.cs.
/// </summary>
public class PasswordHasherService : IPasswordHasher
{
    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        return Hash(password) == passwordHash;
    }
}
