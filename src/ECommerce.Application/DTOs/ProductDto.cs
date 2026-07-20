namespace ECommerce.Application.DTOs;

/// <summary>
/// DTO de respuesta para operaciones sobre productos.
/// Expone únicamente los datos necesarios para el cliente y el segundo microservicio.
/// </summary>
public sealed record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId);
