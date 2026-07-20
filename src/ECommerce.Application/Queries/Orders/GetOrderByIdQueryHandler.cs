using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Queries.Orders;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id);
        if (order is null)
            throw new NotFoundException($"Orden con id {request.Id} no encontrada.");

        // OrderRepository.GetByIdAsync carga OrderItems con Include(ThenInclude(Product)),
        // por lo que oi.Product está disponible para mapear el nombre.
        var items = order.OrderItems.Select(oi => new OrderItemDto(
            oi.Id,
            oi.ProductId,
            oi.Product?.Name ?? string.Empty,
            oi.Quantity,
            oi.UnitPrice,
            oi.Quantity * oi.UnitPrice));

        return new OrderDto(order.Id, order.UserId, order.OrderDate, order.TotalAmount, items);
    }
}
