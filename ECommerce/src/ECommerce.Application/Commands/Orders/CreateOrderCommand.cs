using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Commands.Orders;

public sealed record CreateOrderItemDto(int ProductId, int Quantity);

// FLUJO DE REQUEST (ORDERS - PASO 2): El Command es el paquete de datos que viaja por MediatR.
// Al implementar IRequest<OrderDto>, le indica a MediatR que su Handler devolverá un DTO de tipo OrderDto.
public sealed record CreateOrderCommand(int UserId, List<CreateOrderItemDto> Items) : IRequest<OrderDto>;
