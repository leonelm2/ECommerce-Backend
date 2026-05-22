using ECommerce.Application.Interfaces;
using ECommerce.Application.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Commands.Users;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, User>
{
    private readonly IUserRepository _userRepository;

    public RegisterUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.GetByUsernameAsync(request.Username) is not null)
        {
            throw new DomainRuleException("El nombre de usuario ya está en uso.");
        }

        if (await _userRepository.GetByEmailAsync(request.Email) is not null)
        {
            throw new DomainRuleException("El correo electrónico ya está registrado.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = UserRole.User
        };

        await _userRepository.AddAsync(user);
        return user;
    }
}
