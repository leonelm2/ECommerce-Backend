using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Abstracción para la generación de tokens JWT.
/// Extrae la lógica de seguridad fuera del controller,
/// respetando el principio de responsabilidad única.
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(UserDto user);
}
