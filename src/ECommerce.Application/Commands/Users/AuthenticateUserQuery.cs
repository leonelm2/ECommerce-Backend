using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Commands.Users;

public sealed record AuthenticateUserQuery(string Username, string Password) : IRequest<User>;
