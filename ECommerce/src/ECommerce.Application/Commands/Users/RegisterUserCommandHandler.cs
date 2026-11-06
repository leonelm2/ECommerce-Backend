using ECommerce.Application.DTOs; // Importa UserDto
using ECommerce.Application.Interfaces; // Importa IUserRepository e IPasswordHasher
using ECommerce.Domain.Entities; // Importa la Entidad de Dominio User y el enum UserRole
using ECommerce.Domain.Exceptions; // Importa DomainRuleException
using MediatR; // Importa IRequestHandler

namespace ECommerce.Application.Commands.Users; // Capa Application: caso de uso "Registrar Nuevo Usuario"

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
        // 1. Verifica que no exista ya otro usuario con el mismo Username (si existe, lanza 422 Unprocessable Entity)
        if (await _userRepository.GetByUsernameAsync(request.Username) is not null)
            throw new DomainRuleException("El nombre de usuario ya está en uso.");

        // 2. Verifica que no exista ya otro usuario con el mismo Email (si existe, lanza 422 Unprocessable Entity)
        if (await _userRepository.GetByEmailAsync(request.Email) is not null)
            throw new DomainRuleException("El correo electrónico ya está registrado.");

        // 3. ¡PUNTO CLAVE! Instancia la ENTIDAD DE DOMINIO "User"
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            // Hashea la contraseña antes de guardarla (jamás se guarda la contraseña en texto plano en la BD)
            PasswordHash = _passwordHasher.Hash(request.Password),
            // Por seguridad, todo usuario registrado por la API pública nace con rol estándar "User" (nunca "Admin")
            Role = UserRole.User
        };

        // 4. Guarda la Entidad de Dominio User en la base de datos SQLite
        await _userRepository.AddAsync(user);

        // 5. Crea y devuelve el DTO de salida (UserDto) sin exponer el PasswordHash
        return new UserDto(user.Id, user.Username, user.Email, user.Role.ToString());
    }
}
