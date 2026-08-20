using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.DTOs;
using System;
using System.Threading.Tasks;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestDto request)
    {
        // Simulando un ligero retardo de red/procesamiento
        await Task.Delay(500);

        // Simulando lógica de validación básica
        if (request.Amount <= 0)
        {
            return BadRequest(new ProblemDetails 
            {
                Title = "Error de validación",
                Detail = "El monto a cobrar debe ser mayor a cero."
            });
        }

        // Simulando procesamiento exitoso
        var response = new PaymentResponseDto(
            PaymentId: Guid.NewGuid(),
            OrderId: request.OrderId,
            UserId: request.UserId,
            Status: "Approved",
            TransactionCode: $"TXN-{DateTime.UtcNow.Ticks}",
            Amount: request.Amount,
            Currency: request.Currency,
            ProcessedAt: DateTime.UtcNow,
            Message: "Pago procesado exitosamente"
        );

        return Ok(response);
    }
}
