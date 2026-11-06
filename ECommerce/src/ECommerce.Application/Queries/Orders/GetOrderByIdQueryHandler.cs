using ECommerce.Application.DTOs; // Importa OrderDto y OrderItemDto
using ECommerce.Application.Interfaces; // Importa IOrderRepository
using ECommerce.Domain.Exceptions; // Importa NotFoundException
using MediatR; // Importa IRequestHandler

namespace ECommerce.Application.Queries.Orders; // Capa Application: Query para consultar una orden por su ID

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Busca la orden en la BD (en OrderRepository.GetByIdAsync usa .Include(OrderItems).ThenInclude(Product))
        var order = await _orderRepository.GetByIdAsync(request.Id);
        // Si no existe la orden con ese ID, lanza NotFoundException (404 Not Found)
        if (order is null)
            throw new NotFoundException($"Orden con id {request.Id} no encontrada.");

        // 2. Mapea cada Entidad OrderItem a su respectivo DTO (OrderItemDto) calculando el subtotal (Quantity * UnitPrice)
        var items = order.OrderItems.Select(oi => new OrderItemDto(
            oi.Id,
            oi.ProductId,
            oi.Product?.Name ?? string.Empty,
            oi.Quantity,
            oi.UnitPrice,
            oi.Quantity * oi.UnitPrice));

        // 3. Empaqueta y devuelve el DTO de salida OrderDto
        return new OrderDto(order.Id, order.UserId, order.OrderDate, order.TotalAmount, items, order.Status.ToString());
    }
}
