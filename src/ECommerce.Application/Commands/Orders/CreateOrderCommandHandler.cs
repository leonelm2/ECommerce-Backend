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
    private readonly IPaymentServiceClient _paymentServiceClient;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPaymentServiceClient paymentServiceClient)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _paymentServiceClient = paymentServiceClient;
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
        await _unitOfWork.SaveChangesAsync(); // Generar order.Id en la DB antes de pagar

        try
        {
            // Crear el DTO del request para el microservicio de pagos
            var paymentRequest = new PaymentRequestDto(
                order.Id,
                order.UserId,
                order.TotalAmount,
                "ARS", // Moneda nacional del e-commerce
                $"Pago de Orden #{order.Id} por usuario {order.UserId}"
            );

            // Llamar al microservicio de pagos de forma síncrona/esperando respuesta
            var paymentResult = await _paymentServiceClient.ProcessPaymentAsync(paymentRequest, cancellationToken);

            if (paymentResult.Status == "Approved")
            {
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
            else
            {
                // El pago fue RECHAZADO: Reversión (Rollback manual para mantener la DB limpia y consistente)
                // 1. Restaurar el stock
                foreach (var item in order.OrderItems)
                {
                    if (productMap.TryGetValue(item.ProductId, out var product))
                    {
                        product.Stock += item.Quantity;
                        await _productRepository.UpdateAsync(product, saveChanges: false);
                    }
                }

                // 2. Eliminar la orden creada
                await _orderRepository.DeleteAsync(order.Id, saveChanges: false);
                await _unitOfWork.SaveChangesAsync(); // Persiste los cambios de reversión

                throw new DomainRuleException($"El pago fue rechazado. Motivo: {paymentResult.Message}");
            }
        }
        catch (Exception ex) when (ex is not DomainRuleException && ex is not NotFoundException && ex is not InsufficientStockException)
        {
            // Error de conexión de red, timeout o problemas HTTP en el servicio de pagos
            // Aplicamos reversión preventiva (Rollback) por consistencia del stock y las órdenes
            foreach (var item in order.OrderItems)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                {
                    product.Stock += item.Quantity;
                    await _productRepository.UpdateAsync(product, saveChanges: false);
                }
            }
            await _orderRepository.DeleteAsync(order.Id, saveChanges: false);
            await _unitOfWork.SaveChangesAsync();

            throw new DomainRuleException($"Fallo en la comunicación con el servicio de pagos. La orden fue cancelada por seguridad. Detalle: {ex.Message}");
        }
    }
}
