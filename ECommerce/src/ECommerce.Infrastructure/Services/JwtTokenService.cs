using System.IdentityModel.Tokens.Jwt; // Importa JwtSecurityToken, JwtSecurityTokenHandler y JwtRegisteredClaimNames
using System.Security.Claims; // Importa Claim y ClaimTypes (para meter datos del usuario dentro del payload del JWT)
using System.Text; // Importa Encoding.UTF8 (para convertir la clave secreta string en un array de bytes)
using ECommerce.Application.DTOs; // Importa UserDto
using ECommerce.Application.Interfaces; // Importa la interfaz IJwtTokenService
using ECommerce.Application.Settings; // Importa JwtSettings (la clase que mapea la sección JwtSettings de appsettings.json)
using Microsoft.Extensions.Options; // Importa IOptions<JwtSettings> (Options Pattern)
using Microsoft.IdentityModel.Tokens; // Importa SymmetricSecurityKey, SigningCredentials y SecurityAlgorithms

namespace ECommerce.Infrastructure.Services; // Vive en Infrastructure porque usa librerías criptográficas externas para generar el token

// Implementa la interfaz IJwtTokenService definida en Application
public class JwtTokenService : IJwtTokenService
{
    // Guarda los valores de configuración leídos desde appsettings.json (Secret, Issuer, Audience, ExpiresInMinutes)
    private readonly JwtSettings _jwtSettings;

    // Constructor: recibe IOptions<JwtSettings> inyectado (configurado en Program.cs con builder.Services.Configure<JwtSettings>)
    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        // Extrae el objeto JwtSettings con los valores ya cargados
        _jwtSettings = jwtSettings.Value;
    }

    // Método que recibe el DTO del usuario ya autenticado y devuelve el string del token JWT ("eyJhbGci...")
    public string GenerateToken(UserDto user)
    {
        // 1. CREACIÓN DE CLAIMS (la información pública del usuario que viajará adentro del Payload del token):
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username), // "sub" (Subject): estándar JWT para identificar al sujeto del token
            new Claim(ClaimTypes.Name, user.Username), // Nombre de usuario para User.Identity.Name en .NET
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID numérico del usuario en la base de datos
            new Claim(ClaimTypes.Role, user.Role), // ¡CLAVE! El Rol ("Admin" o "User") que luego lee [Authorize(Roles = "Admin")]
            new Claim(JwtRegisteredClaimNames.Email, user.Email) // Correo electrónico del usuario
        };

        // 2. PREPARACIÓN DE LA FIRMA CRIPTOGRÁFICA:
        // Convierte la clave secreta de appsettings.json en bytes para armar una clave simétrica
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        // Define que el token se firmará usando el algoritmo HMAC-SHA256 con esa clave secreta
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. ARMADO DEL OBJETO TOKEN JWT:
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer, // Emisor del token ("CleanArchitectureApi")
            audience: _jwtSettings.Audience, // Destinatario del token ("CleanArchitectureApiClient")
            claims: claims, // Lista de claims (Id, Username, Role, Email) que creamos arriba
            // Fecha y hora exacta de vencimiento: Hora actual UTC + los minutos configurados en appsettings.json (60 min)
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
            signingCredentials: credentials); // La firma criptográfica para que nadie pueda alterar el rol ni el contenido

        // 4. SERIALIZACIÓN: Convierte el objeto JwtSecurityToken en el string compacto de 3 partes separadas por puntos (Header.Payload.Signature)
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
