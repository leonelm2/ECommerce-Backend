using MediatR; // Importa IMediator para aplicar CQRS también dentro del microservicio de pagos
using Microsoft.AspNetCore.Mvc; // Importa ControllerBase, [ApiController], [Route], [HttpPost], ProblemDetails
using PaymentService.Application.Commands; // Importa ProcessPaymentCommand
using PaymentService.Application.DTOs; // Importa PaymentRequestDto
using System.Threading.Tasks; // Importa Task<IActionResult>

namespace PaymentService.Api.Controllers; // Capa API del segundo microservicio (PaymentService, corre en el puerto 5200)

[ApiController]
[Route("api/[controller]")] // Ruta base: "api/payments"
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator; // Mensajero MediatR del microservicio de pagos

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Endpoint POST /api/payments/process (es el endpoint al que llama PaymentServiceClient desde el backend principal de ECommerce)
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestDto request)
    {
        // 1. Validación de entrada rápida: si el monto a cobrar es 0 o negativo, devuelve HTTP 400 Bad Request con ProblemDetails
        if (request.Amount <= 0)
        {
            return BadRequest(new ProblemDetails 
            {
                Title = "Error de validación",
                Detail = "El monto a cobrar debe ser mayor a cero."
            });
        }

        // 2. Empaqueta el DTO recibido dentro del Command "ProcessPaymentCommand"
        var command = new ProcessPaymentCommand(request);
        // 3. Envía el Command a través de MediatR hacia ProcessPaymentCommandHandler
        var response = await _mediator.Send(command);

        // 4. Devuelve HTTP 200 OK con el PaymentResponseDto (que adentro indica si Status es "Approved" o "Rejected")
        return Ok(response);
    }
}
