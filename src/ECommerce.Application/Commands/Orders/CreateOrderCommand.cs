using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Commands.Orders;

public sealed record CreateOrderItemDto(int ProductId, int Quantity);

public sealed record CreateOrderCommand(int UserId, List<CreateOrderItemDto> Items) : IRequest<OrderDto>;
