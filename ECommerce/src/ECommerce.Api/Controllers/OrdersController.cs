using ECommerce.Application.Commands.Orders;
using ECommerce.Application.DTOs;
using ECommerce.Application.Queries.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        return Ok(order);
    }

    // FLUJO DE REQUEST (ORDERS - PASO 1): El controlador recibe la petición HTTP del cliente con los datos.
    // Su único trabajo es empaquetar esos datos en un Command (CreateOrderCommand) y enviarlo a través de MediatR.
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand request)
    {
        // Envía el Command por MediatR y recibe de vuelta el DTO (OrderDto)
        var order = await _mediator.Send(request);
        // FLUJO DE REQUEST (ORDERS - PASO FINAL): El controlador responde al cliente con un HTTP 201 Created (o 200 OK en GetById) junto con el DTO.
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}
