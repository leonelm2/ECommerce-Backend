namespace ECommerce.Application.DTOs;

/// <summary>
/// DTO de respuesta para operaciones sobre usuarios.
/// Nunca expone PasswordHash ni datos internos del dominio.
/// </summary>
public sealed record UserDto(
    int Id,
    string Username,
    string Email,
    string Role);
