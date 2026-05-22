using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Commands.Orders;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Order>
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

    public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user is null)
        {
            throw new NotFoundException($"Usuario con id {request.UserId} no encontrado.");
        }

        if (request.Items is null || !request.Items.Any())
        {
            throw new DomainRuleException("La orden debe contener al menos un item.");
        }

        var order = new Order
        {
            UserId = request.UserId,
            OrderDate = DateTime.UtcNow,
            OrderItems = new List<OrderItem>()
        };

        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product is null)
            {
                throw new NotFoundException($"Producto con id {item.ProductId} no encontrado.");
            }

            product.ReduceStock(item.Quantity);
            await _productRepository.UpdateAsync(product, saveChanges: false);

            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        order.TotalAmount = order.OrderItems.Sum(x => x.UnitPrice * x.Quantity);
        await _orderRepository.AddAsync(order, saveChanges: false);
        await _unitOfWork.SaveChangesAsync();

        return order;
    }
}
