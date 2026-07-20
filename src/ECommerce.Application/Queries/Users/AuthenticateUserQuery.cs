using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Queries.Users;

/// <summary>
/// Query para autenticar un usuario (login).
/// Movida de Commands/ a Queries/ — el login es una operación de lectura/validación,
/// no modifica estado, por lo que correctamente es una Query en CQRS.
/// </summary>
public sealed record AuthenticateUserQuery(string Username, string Password) : IRequest<UserDto>;
