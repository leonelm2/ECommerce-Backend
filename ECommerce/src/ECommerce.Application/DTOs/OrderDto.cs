namespace ECommerce.Application.DTOs;

public sealed record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record OrderDto(
    int Id,
    int UserId,
    DateTime OrderDate,
    decimal TotalAmount,
    IEnumerable<OrderItemDto> Items,
    string Status);
