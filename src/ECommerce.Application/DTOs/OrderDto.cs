namespace ECommerce.Application.DTOs;

/// <summary>
/// DTO de respuesta para un ítem de una orden.
/// Incluye el nombre del producto para facilitar la integración con otros microservicios.
/// </summary>
public sealed record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

/// <summary>
/// DTO de respuesta para operaciones sobre órdenes.
/// Evita referencias circulares de las entidades de dominio al serializar.
/// </summary>
public sealed record OrderDto(
    int Id,
    int UserId,
    DateTime OrderDate,
    decimal TotalAmount,
    IEnumerable<OrderItemDto> Items);
