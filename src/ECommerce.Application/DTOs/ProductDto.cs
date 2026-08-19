namespace ECommerce.Application.DTOs;

public sealed record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId);
