using ECommerce.Application.DTOs; // Importa los DTOs (OrderDto, OrderItemDto, PaymentRequestDto)
using ECommerce.Application.Interfaces; // Importa las interfaces de repositorios, UnitOfWork y PaymentServiceClient
using ECommerce.Domain.Entities; // Importa las Entidades de Dominio (Order, OrderItem, OrderStatus, Product)
using ECommerce.Domain.Exceptions; // Importa las excepciones de dominio (NotFoundException, DomainRuleException, InsufficientStockException)
using MediatR; // Importa IRequestHandler de MediatR para implementar el caso de uso CQRS

namespace ECommerce.Application.Commands.Orders; // Capa de Aplicación: contiene la lógica de orquestación del caso de uso "Crear Orden"

// Clase Handler que procesa el comando CreateOrderCommand y devuelve un OrderDto.
// MediatR la encuentra automáticamente gracias a que implementa IRequestHandler<CreateOrderCommand, OrderDto>.
public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    // Dependencias privadas (todas son INTERFACES de la capa Application para respetar la Regla de Dependencia)
    private readonly IOrderRepository _orderRepository; // Contrato para acceder a la tabla de Órdenes
    private readonly IProductRepository _productRepository; // Contrato para acceder a la tabla de Productos
    private readonly IUserRepository _userRepository; // Contrato para acceder a la tabla de Usuarios
    private readonly IUnitOfWork _unitOfWork; // Contrato para guardar cambios en la BD en una sola transacción (SaveChangesAsync)
    private readonly IPaymentServiceClient _paymentServiceClient; // Contrato para llamar al microservicio externo de pagos

    // Constructor: recibe todas las dependencias por Inyección de Dependencias (registradas en InfrastructureServiceExtensions.cs)
    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPaymentServiceClient paymentServiceClient)
    {
        _orderRepository = orderRepository; // Asigna el repositorio de órdenes inyectado
        _productRepository = productRepository; // Asigna el repositorio de productos inyectado
        _userRepository = userRepository; // Asigna el repositorio de usuarios inyectado
        _unitOfWork = unitOfWork; // Asigna la unidad de trabajo inyectada
        _paymentServiceClient = paymentServiceClient; // Asigna el cliente HTTP de pagos inyectado
    }

    // Método principal que ejecuta MediatR cuando el OrdersController hace "_mediator.Send(request)"
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // ================= FASE 1: VALIDACIÓN DE EXISTENCIA Y RESERVA LOCAL DE STOCK =================

        // Busca en la base de datos si existe el usuario que quiere hacer la compra
        var user = await _userRepository.GetByIdAsync(request.UserId);
        // Si el usuario no existe en la BD, lanza NotFoundException (GlobalExceptionHandler devolverá 404 Not Found)
        if (user is null)
            throw new NotFoundException($"Usuario con id {request.UserId} no encontrado.");

        // Valida por seguridad que la lista de ítems no sea nula ni esté vacía (si lo está, lanza 422 Unprocessable Entity)
        if (request.Items is null || !request.Items.Any())
            throw new DomainRuleException("La orden debe contener al menos un item.");

        // Extrae todos los IDs de productos distintos que vienen en el pedido para buscarlos de una sola vez
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        // Trae todos los productos juntos de la BD (evita el problema de N+1 queries) y los guarda en un Diccionario en memoria (Clave: Id, Valor: Entidad Product)
        var productMap = (await _productRepository.GetByIdsAsync(productIds))
            .ToDictionary(p => p.Id);

        // ¡PUNTO CLAVE DE EXAMEN! Acá se instancia la ENTIDAD DE DOMINIO "Order" (en estado inicial Pending)
        var order = new Order
        {
            UserId = request.UserId, // Asigna el ID del usuario comprador
            OrderDate = DateTime.UtcNow, // Guarda la fecha y hora actual en formato UTC estándar
            Status = OrderStatus.Pending, // Estado inicial: Pendiente de pago
            OrderItems = new List<OrderItem>() // Inicializa la colección vacía de ítems de la orden
        };

        // Recorre cada ítem que el cliente pidió en el Command
        foreach (var item in request.Items)
        {
            // Busca el producto en el diccionario en memoria; si no existe en la BD, lanza NotFoundException (404)
            if (!productMap.TryGetValue(item.ProductId, out var product))
                throw new NotFoundException($"Producto con id {item.ProductId} no encontrado.");

            // ¡REGLA DE DOMINIO RICO! Llama al método de la Entidad de Dominio Product.
            // Si no hay stock suficiente, product.ReduceStock lanza InsufficientStockException (422) y corta todo antes de tocar la BD.
            product.ReduceStock(item.Quantity);
            // Marca la entidad Product como modificada en EF Core, pero con "saveChanges: false" para NO impactar la BD todavía
            await _productRepository.UpdateAsync(product, saveChanges: false);

            // Crea la Entidad de Dominio hija "OrderItem" y la agrega a la orden
            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId, // ID del producto comprado
                Quantity = item.Quantity, // Cantidad comprada
                UnitPrice = product.Price // Congela el precio unitario actual del producto en la BD (no confía en precios enviados por el cliente)
            });
        }

        // Calcula el monto total de la orden sumando (Precio Unitario * Cantidad) de cada ítem
        order.TotalAmount = order.OrderItems.Sum(x => x.UnitPrice * x.Quantity);
        // Agrega la entidad Order al contexto de EF Core con "saveChanges: false" (aún en memoria)
        await _orderRepository.AddAsync(order, saveChanges: false);
        // ¡UNIT OF WORK! Ejecuta un único SaveChangesAsync() atómico en la BD: guarda la orden en Pending, sus ítems y el descuento de stock todo junto
        await _unitOfWork.SaveChangesAsync();

        // ================= FASE 2: COMUNICACIÓN CON EL MICROSERVICIO DE PAGOS =================
        try
        {
            // Empaqueta los datos necesarios para cobrar dentro de un DTO de contrato (PaymentRequestDto)
            var paymentRequest = new PaymentRequestDto(
                order.Id, // ID de la orden recién generada por la base de datos
                order.UserId, // ID del usuario
                order.TotalAmount, // Monto total a cobrar
                "ARS", // Moneda
                $"Pago de Orden #{order.Id} por usuario {order.UserId}" // Descripción del cobro
            );

            // Llama por HTTP al microservicio PaymentService a través de la interfaz IPaymentServiceClient
            var paymentResult = await _paymentServiceClient.ProcessPaymentAsync(paymentRequest, cancellationToken);

            // ================= FASE 3A: CAMINO FELIZ (PAGO APROBADO < $100.000) =================
            if (paymentResult.Status == "Approved")
            {
                // Llama al método de Dominio Rico de la entidad Order para pasar el estado a "Paid" y guardar el código de transacción
                order.MarkAsPaid(paymentResult.TransactionCode ?? Guid.NewGuid().ToString());
                // Marca la orden como modificada en memoria
                await _orderRepository.UpdateAsync(order, saveChanges: false);
                // Confirma el cambio de estado a "Paid" en la base de datos SQLite
                await _unitOfWork.SaveChangesAsync();

                // Mapea cada entidad OrderItem a su DTO de salida (OrderItemDto) para armar la respuesta
                var itemDtos = order.OrderItems.Select(oi => new OrderItemDto(
                    oi.Id, // ID del ítem de orden
                    oi.ProductId, // ID del producto
                    productMap.TryGetValue(oi.ProductId, out var p) ? p.Name : string.Empty, // Nombre del producto desde el diccionario
                    oi.Quantity, // Cantidad
                    oi.UnitPrice, // Precio unitario
                    oi.Quantity * oi.UnitPrice)); // Subtotal del ítem

                // ¡PUNTO CLAVE DE EXAMEN! Crea y devuelve el DTO DE SALIDA (OrderDto) a MediatR (que se lo devolverá al Controller)
                return new OrderDto(order.Id, order.UserId, order.OrderDate, order.TotalAmount, itemDtos, order.Status.ToString());
            }
            // ================= FASE 3B: CAMINO DE RECHAZO (PAGO RECHAZADO >= $100.000) =================
            else
            {
                // COMPENSACIÓN (Patrón Saga): Llama al método de dominio para cambiar el estado de la orden a "PaymentRejected" y guardar el motivo
                order.MarkPaymentAsRejected(paymentResult.Message ?? "Pago rechazado por PaymentService.");
                // Marca la orden para actualizar en memoria
                await _orderRepository.UpdateAsync(order, saveChanges: false);

                // Recorre cada ítem de la orden para DEVOLVER EL STOCK que habíamos descontado en la Fase 1
                foreach (var item in order.OrderItems)
                {
                    if (productMap.TryGetValue(item.ProductId, out var product))
                    {
                        product.Stock += item.Quantity; // Restituye la cantidad al stock del producto
                        await _productRepository.UpdateAsync(product, saveChanges: false); // Marca el producto para actualizar
                    }
                }

                // Guarda en una sola transacción el estado PaymentRejected y la devolución del stock en la BD
                await _unitOfWork.SaveChangesAsync();

                // Lanza DomainRuleException para que GlobalExceptionHandler responda 422 Unprocessable Entity al cliente
                throw new DomainRuleException($"El pago fue rechazado. Motivo: {paymentResult.Message}");
            }
        }
        // ================= FASE 3C: RESILIENCIA ANTE CAÍDA DEL MICROSERVICIO DE PAGOS =================
        catch (HttpRequestException ex) // Se ejecuta si PaymentService está apagado o hay fallo de red (después de los 3 reintentos de Polly)
        {
            // COMPENSACIÓN: Marca la orden como rechazada por fallo de comunicación
            order.MarkPaymentAsRejected("Error de comunicación con PaymentService: Servicio no disponible o timeout.");
            await _orderRepository.UpdateAsync(order, saveChanges: false);

            // Devuelve el stock reservado a cada producto
            foreach (var item in order.OrderItems)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                {
                    product.Stock += item.Quantity;
                    await _productRepository.UpdateAsync(product, saveChanges: false);
                }
            }

            // Guarda el rollback de negocio en la base de datos
            await _unitOfWork.SaveChangesAsync();

            // Lanza excepción de regla de dominio (422) informando que el servicio de pagos falló pero el sistema principal sigue íntegro
            throw new DomainRuleException($"Fallo en la comunicación con el servicio de pagos: {ex.Message}");
        }
        // ================= FASE 3D: RESILIENCIA ANTE TIMEOUT DEL MICROSERVICIO =================
        catch (TaskCanceledException ex) // Se ejecuta si PaymentService tarda más de los 10 segundos configurados en el HttpClient
        {
            // COMPENSACIÓN: Marca la orden como rechazada por Timeout
            order.MarkPaymentAsRejected("Timeout de comunicación con PaymentService.");
            await _orderRepository.UpdateAsync(order, saveChanges: false);

            // Devuelve el stock reservado a cada producto
            foreach (var item in order.OrderItems)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                {
                    product.Stock += item.Quantity;
                    await _productRepository.UpdateAsync(product, saveChanges: false);
                }
            }

            // Guarda la compensación en la base de datos
            await _unitOfWork.SaveChangesAsync();

            // Lanza excepción de regla de dominio (422) informando el timeout
            throw new DomainRuleException($"Timeout al contactar servicio de pagos: {ex.Message}");
        }
        // ================= FASE 3E: RED DE SEGURIDAD ANTE CUALQUIER OTRO ERROR INESPERADO =================
        // El filtro "when" evita volver a capturar las excepciones de dominio que nosotros mismos lanzamos arriba
        catch (Exception ex) when (ex is not DomainRuleException && ex is not NotFoundException && ex is not InsufficientStockException)
        {
            // Cancela la orden usando el método de dominio Cancel() (pasa a estado Cancelled)
            order.Cancel();
            await _orderRepository.UpdateAsync(order, saveChanges: false);

            // Devuelve el stock a los productos por seguridad
            foreach (var item in order.OrderItems)
            {
                if (productMap.TryGetValue(item.ProductId, out var product))
                {
                    product.Stock += item.Quantity;
                    await _productRepository.UpdateAsync(product, saveChanges: false);
                }
            }

            // Guarda la cancelación y la devolución de stock en la BD
            await _unitOfWork.SaveChangesAsync();

            // Lanza DomainRuleException explicando que la orden fue cancelada de forma segura
            throw new DomainRuleException($"Fallo interno. La orden fue cancelada por seguridad. Detalle: {ex.Message}");
        }
    }
}
