namespace ECommerce.Application.Settings;

public class JwtSettings
{
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;

    // DURACIÓN DEL JWT (1/3): Propiedad que mapea el valor "JwtSettings:ExpiresInMinutes" de appsettings.json (línea 12, donde se cambia el número de minutos, ej: 60).
    public int ExpiresInMinutes { get; set; }
}
