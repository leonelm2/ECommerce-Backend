using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Queries.Users;

public sealed record AuthenticateUserQuery(string Username, string Password) : IRequest<UserDto>;
