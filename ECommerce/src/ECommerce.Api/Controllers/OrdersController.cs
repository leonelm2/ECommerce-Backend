using ECommerce.Application.Commands.Orders; // Importa CreateOrderCommand
using ECommerce.Application.DTOs; // Importa OrderDto
using ECommerce.Application.Queries.Orders; // Importa GetOrderByIdQuery
using MediatR; // Importa IMediator para desacoplar el Controller de los Handlers
using Microsoft.AspNetCore.Authorization; // Importa el atributo [Authorize] para exigir token JWT
using Microsoft.AspNetCore.Http; // Importa StatusCodes (201, 400, 401, 404, 422)
using Microsoft.AspNetCore.Mvc; // Importa ControllerBase, [ApiController], [Route], [HttpGet], [HttpPost], IActionResult

namespace ECommerce.Api.Controllers; // Capa de Presentación/API: punto de entrada HTTP para las órdenes

[ApiController] // Habilita comportamientos de API REST (como validación de modelo y binding automático de [FromBody])
[Route("api/[controller]")] // Define la ruta base como "api/orders" (toma el nombre del controlador sin la palabra Controller)
[Authorize] // Exige que el cliente envíe un token JWT válido en el header "Authorization: Bearer <token>" para cualquier endpoint de esta clase
public class OrdersController : ControllerBase
{
    // Dependencia privada hacia MediatR. ¡Fijate que el Controller NO inyecta ningún repositorio ni DbContext! (Thin Controller)
    private readonly IMediator _mediator;

    // Constructor: recibe IMediator por Inyección de Dependencias
    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Endpoint GET /api/orders/{id} (ejemplo: GET /api/orders/1)
    // El filtro ":int" en la ruta asegura que el parámetro id sea un número entero
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        // Empaqueta el id en una Query de lectura (GetOrderByIdQuery) y la envía por MediatR hacia GetOrderByIdQueryHandler
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        // Devuelve HTTP 200 OK con el OrderDto serializado a JSON
        return Ok(order);
    }

    // Endpoint POST /api/orders para crear una nueva orden de compra
    [HttpPost]
    // Estos atributos [ProducesResponseType] documentan en Swagger todos los posibles códigos de respuesta y sus tipos
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)] // 201 si la orden se creó y pagó con éxito
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)] // 400 si falla FluentValidation (ej. cantidad <= 0)
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)] // 401 si no envió token JWT válido
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)] // 404 si el usuario o algún producto no existe
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)] // 422 si no hay stock o el pago fue rechazado
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand request) // [FromBody] deserializa el JSON del cliente a un objeto COMMAND (CreateOrderCommand)
    {
        // Envía el Command a través de MediatR:
        // 1° Pasa por ValidationBehavior (FluentValidation).
        // 2° Llega a CreateOrderCommandHandler, que crea la Entidad de Dominio Order, llama a pagos y devuelve el DTO OrderDto.
        var order = await _mediator.Send(request);
        // Devuelve HTTP 201 Created, agrega el header "Location: /api/orders/{id}" apuntando al método GetById, y envía el OrderDto en el cuerpo JSON
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}
