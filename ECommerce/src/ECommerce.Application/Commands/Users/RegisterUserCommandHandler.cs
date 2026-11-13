using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Commands.Users;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.GetByUsernameAsync(request.Username) is not null)
            throw new DomainRuleException("El nombre de usuario ya está en uso.");

        if (await _userRepository.GetByEmailAsync(request.Email) is not null)
            throw new DomainRuleException("El correo electrónico ya está registrado.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.User
        };

        await _userRepository.AddAsync(user);

        return new UserDto(user.Id, user.Username, user.Email, user.Role.ToString());
    }
}
