using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Commands.Orders;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user is null)
            throw new NotFoundException($"Usuario con id {request.UserId} no encontrado.");

        if (request.Items is null || !request.Items.Any())
            throw new DomainRuleException("La orden debe contener al menos un item.");

        // FIX N+1: cargar TODOS los productos necesarios en una única query SQL (IN clause)
        // en lugar de hacer una query por cada ítem del pedido.
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productMap = (await _productRepository.GetByIdsAsync(productIds))
            .ToDictionary(p => p.Id);

        var order = new Order
        {
            UserId = request.UserId,
            OrderDate = DateTime.UtcNow,
            OrderItems = new List<OrderItem>()
        };

        foreach (var item in request.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
                throw new NotFoundException($"Producto con id {item.ProductId} no encontrado.");

            // Lógica de dominio: ReduceStock valida stock y lanza InsufficientStockException
            product.ReduceStock(item.Quantity);
            await _productRepository.UpdateAsync(product, saveChanges: false);

            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price  // Precio capturado al momento de la compra
            });
        }

        order.TotalAmount = order.OrderItems.Sum(x => x.UnitPrice * x.Quantity);
        await _orderRepository.AddAsync(order, saveChanges: false);
        await _unitOfWork.SaveChangesAsync();

        // Mapear a DTO usando el productMap (ya cargado en memoria, sin queries adicionales)
        var itemDtos = order.OrderItems.Select(oi => new OrderItemDto(
            oi.Id,
            oi.ProductId,
            productMap.TryGetValue(oi.ProductId, out var p) ? p.Name : string.Empty,
            oi.Quantity,
            oi.UnitPrice,
            oi.Quantity * oi.UnitPrice));

        return new OrderDto(order.Id, order.UserId, order.OrderDate, order.TotalAmount, itemDtos);
    }
}
