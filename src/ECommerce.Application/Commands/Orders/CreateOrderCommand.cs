using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Commands.Orders;

/// <summary>
/// DTO de entrada para los ítems de la orden (no confundir con OrderItemDto de respuesta).
/// </summary>
public sealed record CreateOrderItemDto(int ProductId, int Quantity);

public sealed record CreateOrderCommand(int UserId, List<CreateOrderItemDto> Items) : IRequest<OrderDto>;
