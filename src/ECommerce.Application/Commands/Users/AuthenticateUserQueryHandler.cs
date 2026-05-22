using ECommerce.Application.Interfaces;
using ECommerce.Application.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Commands.Users;

public sealed class AuthenticateUserQueryHandler : IRequestHandler<AuthenticateUserQuery, User>
{
    private readonly IUserRepository _userRepository;

    public AuthenticateUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> Handle(AuthenticateUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Usuario o contraseña incorrectos.");
        }

        return user;
    }
}
