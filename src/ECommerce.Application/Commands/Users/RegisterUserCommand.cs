using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Commands.Users;

public sealed record RegisterUserCommand(string Username, string Email, string Password) : IRequest<UserDto>;
