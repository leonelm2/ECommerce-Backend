namespace ECommerce.Application.Settings;

/// <summary>
/// Configuración del token JWT.
/// Ubicada en Application para que Infrastructure pueda acceder a ella
/// sin crear dependencias hacia la capa Api.
/// </summary>
public class JwtSettings
{
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiresInMinutes { get; set; }
}
