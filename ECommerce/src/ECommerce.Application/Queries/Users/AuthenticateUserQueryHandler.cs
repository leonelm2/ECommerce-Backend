using ECommerce.Application.DTOs; // Importa UserDto
using ECommerce.Application.Interfaces; // Importa IUserRepository e IPasswordHasher
using ECommerce.Domain.Exceptions; // Importa AuthenticationException
using MediatR; // Importa IRequestHandler

namespace ECommerce.Application.Queries.Users; // Capa Application: Query para autenticar usuario en el Login

public sealed class AuthenticateUserQueryHandler : IRequestHandler<AuthenticateUserQuery, UserDto>
{
    private readonly IUserRepository _userRepository; // Repositorio para buscar al usuario por su Username
    private readonly IPasswordHasher _passwordHasher; // Servicio para comparar la contraseña en texto plano contra el Hash guardado en la BD

    // Constructor: recibe ambas dependencias por Inyección de Dependencias
    public AuthenticateUserQueryHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    // Método que ejecuta MediatR cuando AuthController.Login hace "_mediator.Send(request)"
    public async Task<UserDto> Handle(AuthenticateUserQuery request, CancellationToken cancellationToken)
    {
        // 1. Busca la Entidad de Dominio User en la tabla Users por nombre de usuario
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        // 2. Si el usuario no existe O la contraseña ingresada no coincide con el PasswordHash de la BD:
        // Lanza AuthenticationException (que GlobalExceptionHandler traduce a un HTTP 401 Unauthorized).
        // Nota de seguridad: da el mismo mensaje genérico ("Usuario o contraseña incorrectos") para no revelar si el usuario existe o no.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AuthenticationException("Usuario o contraseña incorrectos.");

        // 3. Si las credenciales son válidas, empaqueta los datos públicos en un UserDto (¡sin el PasswordHash!) y se lo devuelve al AuthController
        return new UserDto(user.Id, user.Username, user.Email, user.Role.ToString());
    }
}
