using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands;
using PaymentService.Application.DTOs;
using System.Threading.Tasks;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequestDto request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new ProblemDetails 
            {
                Title = "Error de validación",
                Detail = "El monto a cobrar debe ser mayor a cero."
            });
        }

        var command = new ProcessPaymentCommand(request);
        var response = await _mediator.Send(command);

        return Ok(response);
    }
}
