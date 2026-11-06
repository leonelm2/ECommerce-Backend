using ECommerce.Application.Commands.Users; // Importa RegisterUserCommand
using ECommerce.Application.DTOs; // Importa UserDto
using ECommerce.Application.Interfaces; // Importa IJwtTokenService (para generar el token JWT tras validar credenciales)
using ECommerce.Application.Queries.Users; // Importa AuthenticateUserQuery
using MediatR; // Importa IMediator
using Microsoft.AspNetCore.Authorization; // Importa [AllowAnonymous]
using Microsoft.AspNetCore.Http; // Importa StatusCodes
using Microsoft.AspNetCore.Mvc; // Importa ControllerBase, [ApiController], [Route], etc.

namespace ECommerce.Api.Controllers; // Capa API: controlador encargado de Registro y Login de usuarios

[ApiController]
[Route("api/[controller]")] // Ruta base: "api/auth"
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator; // Mensajero MediatR para enviar el comando de registro o la query de login
    private readonly IJwtTokenService _jwtTokenService; // Servicio que firma y genera el string del token JWT

    // Constructor: recibe MediatR y el servicio de JWT por Inyección de Dependencias
    public AuthController(IMediator mediator, IJwtTokenService jwtTokenService)
    {
        _mediator = mediator;
        _jwtTokenService = jwtTokenService;
    }

    // Endpoint POST /api/auth/register (registra un nuevo usuario con rol "User")
    [HttpPost("register")]
    [AllowAnonymous] // Es público porque el usuario todavía no tiene cuenta ni token
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand request) // Deserializa el JSON en el Command RegisterUserCommand
    {
        // Envía el Command a RegisterUserCommandHandler (que valida duplicados, hashea la password, crea la Entidad User y devuelve UserDto)
        var userDto = await _mediator.Send(request);
        // Devuelve HTTP 201 Created con los datos públicos del usuario (UserDto, sin la contraseña)
        return CreatedAtAction(nameof(Register), new { id = userDto.Id }, userDto);
    }

    // Endpoint POST /api/auth/login (autentica al usuario y le entrega su token JWT)
    [HttpPost("login")]
    [AllowAnonymous] // Es público porque justamente acá el usuario viene a obtener su token
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthenticateUserQuery request) // Recibe Username y Password en AuthenticateUserQuery
    {
        // 1. Envía la Query a AuthenticateUserQueryHandler.
        // Si el usuario no existe o la contraseña no coincide con el Hash, el Handler lanza AuthenticationException (401 Unauthorized).
        var userDto = await _mediator.Send(request);
        // 2. Si las credenciales son válidas, llama a JwtTokenService para generar el token JWT firmado con los claims del usuario (Id, Username, Email, Role)
        var token = _jwtTokenService.GenerateToken(userDto);
        // 3. Devuelve HTTP 200 OK con un objeto JSON que contiene el Token y los datos básicos del usuario
        return Ok(new { Token = token, userDto.Username, userDto.Email, userDto.Role });
    }
}
