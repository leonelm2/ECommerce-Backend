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

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productMap = (await _productRepository.GetByIdsAsync(productIds))
            .ToDictionary(p => p.Id);

        var order = new Order
        {
            UserId = request.UserId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderItems = new List<OrderItem>()
        };

        foreach (var item in request.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
                throw new NotFoundException($"Producto con id {item.ProductId} no encontrado.");

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

        try
        {
            var paymentRequest = new PaymentRequestDto(
                order.Id,
                order.UserId,
                order.TotalAmount,
                "ARS",
                $"Pago de Orden #{order.Id} por usuario {order.UserId}"
            );

            var paymentResult = await _paymentServiceClient.ProcessPaymentAsync(paymentRequest, cancellationToken);

            if (paymentResult.Status == "Approved")
            {
                order.MarkAsPaid(paymentResult.TransactionCode ?? Guid.NewGuid().ToString());
                await _orderRepository.UpdateAsync(order, saveChanges: false);
                await _unitOfWork.SaveChangesAsync();

                var itemDtos = order.OrderItems.Select(oi => new OrderItemDto(
                    oi.Id,
                    oi.ProductId,
                    productMap.TryGetValue(oi.ProductId, out var p) ? p.Name : string.Empty,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.Quantity * oi.UnitPrice));

                return new OrderDto(order.Id, order.UserId, order.OrderDate, order.TotalAmount, itemDtos, order.Status.ToString());
            }
            else
            {
                order.MarkPaymentAsRejected(paymentResult.Message ?? "Pago rechazado por PaymentService.");
                await _orderRepository.UpdateAsync(order, saveChanges: false);

                foreach (var item in order.OrderItems)
                {
                    if (productMap.TryGetValue(item.ProductId, out var product))
                    {
                        product.Stock += item.Quantity;
                        await _productRepository.UpdateAsync(product, saveChanges: false);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                throw new DomainRuleException($"El pago fue rechazado. Motivo: {paymentResult.Message}");
            }
        }
        catch (HttpRequestException ex)
        {
            order.MarkPaymentAsRejected("Error de comunicación con PaymentService: Servicio no disponible o timeout.");
            await _orderRepository.UpdateAsync(order, saveChanges: false);

            foreach (var item in order.OrderItems)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                {
                    product.Stock += item.Quantity;
                    await _productRepository.UpdateAsync(product, saveChanges: false);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            throw new DomainRuleException($"Fallo en la comunicación con el servicio de pagos: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            order.MarkPaymentAsRejected("Timeout de comunicación con PaymentService.");
            await _orderRepository.UpdateAsync(order, saveChanges: false);

            foreach (var item in order.OrderItems)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                {
                    product.Stock += item.Quantity;
                    await _productRepository.UpdateAsync(product, saveChanges: false);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            throw new DomainRuleException($"Timeout al contactar servicio de pagos: {ex.Message}");
        }
        catch (Exception ex) when (ex is not DomainRuleException && ex is not NotFoundException && ex is not InsufficientStockException)
        {
            order.Cancel();
            await _orderRepository.UpdateAsync(order, saveChanges: false);

            foreach (var item in order.OrderItems)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                {
                    product.Stock += item.Quantity;
                    await _productRepository.UpdateAsync(product, saveChanges: false);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            throw new DomainRuleException($"Fallo interno. La orden fue cancelada por seguridad. Detalle: {ex.Message}");
        }
    }
}
