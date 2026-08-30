using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PaymentService.Api.Core.Application.DTOs;
using PaymentService.Api.Core.Domain.Entities;

namespace PaymentService.Api.Core.Application.Commands;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentResponseDto>
{
    public async Task<PaymentResponseDto> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // Simulando un ligero retardo de red/procesamiento
        await Task.Delay(500, cancellationToken);

        var req = request.Request;
        
        var payment = new Payment(req.Amount);

        if (payment.IsApproved())
        {
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
