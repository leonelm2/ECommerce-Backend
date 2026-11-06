using System; // Importa Guid y DateTime
using System.Threading; // Importa CancellationToken
using System.Threading.Tasks; // Importa Task
using MediatR; // Importa IRequestHandler
using PaymentService.Application.DTOs; // Importa PaymentResponseDto
using PaymentService.Domain.Entities; // Importa la Entidad de Dominio Payment

namespace PaymentService.Application.Commands; // Capa Application del microservicio PaymentService

// Handler que procesa el cobro en el microservicio de pagos
public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentResponseDto>
{
    public async Task<PaymentResponseDto> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Simula una demora realista de 500 milisegundos (medio segundo) de una pasarela bancaria externa
        await Task.Delay(500, cancellationToken);

        // Extrae los datos del pedido de pago (OrderId, UserId, Amount, Currency)
        var req = request.Request;
        
        // 2. ¡PUNTO CLAVE DE EXAMEN! Instancia la ENTIDAD DE DOMINIO "Payment" pasándole el monto a cobrar
        var payment = new Payment(req.Amount);

        // 3. Llama al método de Dominio Rico "payment.IsApproved()" (que evalúa la regla de negocio: Amount < 100.000)
        if (payment.IsApproved())
        {
            // Si el monto es menor a $100.000, crea y devuelve el DTO de respuesta con Status: "Approved" y un código de transacción único
            return new PaymentResponseDto(
                PaymentId: Guid.NewGuid(),
                OrderId: req.OrderId,
                UserId: req.UserId,
                Status: "Approved",
                TransactionCode: $"TXN-{DateTime.UtcNow.Ticks}",
                Amount: req.Amount,
                Currency: req.Currency,
                ProcessedAt: DateTime.UtcNow,
                Message: "Pago procesado exitosamente"
            );
        }
        else
        {
            // Si el monto es >= $100.000, crea y devuelve el DTO de respuesta con Status: "Rejected"
            // (Cuando ECommerce reciba este Status "Rejected", ejecutará la compensación devolviendo el stock a los productos)
            return new PaymentResponseDto(
                PaymentId: Guid.NewGuid(),
                OrderId: req.OrderId,
                UserId: req.UserId,
                Status: "Rejected",
                TransactionCode: $"TXN-{DateTime.UtcNow.Ticks}",
                Amount: req.Amount,
                Currency: req.Currency,
                ProcessedAt: DateTime.UtcNow,
                Message: "Pago rechazado. El monto supera el límite permitido."
            );
        }
    }
}
