namespace ECommerce.Application.Interfaces;

/// <summary>
/// Abstracción para el hashing de contraseñas.
/// Permite inyección de dependencias y facilita el testing con mocks.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
