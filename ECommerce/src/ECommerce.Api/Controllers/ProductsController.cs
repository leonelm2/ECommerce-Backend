using ECommerce.Application.Commands.Products; // Importa CreateProductCommand, UpdateProductCommand, DeleteProductCommand
using ECommerce.Application.DTOs; // Importa ProductDto
using ECommerce.Application.Queries.Products; // Importa GetAllProductsQuery y GetProductByIdQuery
using ECommerce.Domain.Entities; // Importa UserRoles (para restringir acciones de escritura al rol Admin)
using MediatR; // Importa IMediator (patrón Mediator para CQRS)
using Microsoft.AspNetCore.Authorization; // Importa [Authorize] y [AllowAnonymous]
using Microsoft.AspNetCore.Http; // Importa StatusCodes
using Microsoft.AspNetCore.Mvc; // Importa ControllerBase, [ApiController], [Route], etc.

namespace ECommerce.Api.Controllers; // Capa API: expone los endpoints HTTP de Productos

[ApiController] // Indica que es un controlador de API REST
[Route("api/[controller]")] // Ruta base: "api/products"
[Authorize] // Por defecto pide estar autenticado, salvo en los métodos que tengan [AllowAnonymous]
public class ProductsController : ControllerBase
{
    // El controlador solo conoce a MediatR (no conoce repositorios ni lógica de negocio)
    private readonly IMediator _mediator;

    // Constructor que inyecta IMediator
    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Endpoint GET /api/products (lista todos los productos del catálogo)
    [HttpGet]
    [AllowAnonymous] // Permite que cualquier visitante vea el catálogo sin necesidad de estar logueado ni enviar token JWT
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        // Crea la Query de lectura GetAllProductsQuery y la envía a GetAllProductsQueryHandler mediante MediatR
        var products = await _mediator.Send(new GetAllProductsQuery());
        // Devuelve HTTP 200 OK con la lista de ProductDto en JSON
        return Ok(products);
    }

    // Endpoint GET /api/products/{id} (busca un producto específico por su ID)
    [HttpGet("{id:int}")]
    [AllowAnonymous] // Cualquier usuario puede consultar el detalle de un producto sin token JWT
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        // Empaqueta el ID en GetProductByIdQuery y lo envía a GetProductByIdQueryHandler
        // (Si no existe, el Handler lanza NotFoundException y GlobalExceptionHandler responde 404)
        var product = await _mediator.Send(new GetProductByIdQuery(id));
        // Devuelve HTTP 200 OK con el ProductDto encontrado
        return Ok(product);
    }

    // PASO 1: EL CONTROLADOR RECIBE LA PETICIÓN HTTP
    // Cuando el cliente envía un POST a /api/products, ASP.NET Core mapea el JSON del body a la clase "CreateProductCommand" (request).
    // El controlador NO tiene lógica de base de datos. Solo recibe el paquete y se lo entrega al mensajero (MediatR) con _mediator.Send(request).
    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)] // AUTORIZACIÓN POR ROL: Solo un usuario cuyo token JWT tenga el claim Role = "Admin" puede crear productos (si es "User", recibe 403 Forbidden)
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand request) // Deserializa el JSON del body al Command CreateProductCommand
    {
        // Envía el Command a través de MediatR (valida en ValidationBehavior y ejecuta CreateProductCommandHandler)
        var product = await _mediator.Send(request);
        // Devuelve HTTP 201 Created con el ProductDto creado y el header Location apuntando a GetById
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // Endpoint PUT /api/products/{id} para actualizar un producto existente
    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRoles.Admin)] // Solo administradores pueden modificar productos
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductCommand request)
    {
        // Verifica que el ID enviado en la URL (/api/products/5) coincida con el ID dentro del JSON del cuerpo
        if (id != request.Id)
        {
            // Si no coinciden, devuelve 400 Bad Request en formato ProblemDetails
            return BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = "El ID de la ruta no coincide con el ID del cuerpo.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Envía el UpdateProductCommand a UpdateProductCommandHandler mediante MediatR
        var product = await _mediator.Send(request);
        // Devuelve HTTP 200 OK con el ProductDto actualizado
        return Ok(product);
    }

    // Endpoint DELETE /api/products/{id} para eliminar un producto
    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRoles.Admin)] // Solo administradores pueden borrar productos
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        // Empaqueta el id en un DeleteProductCommand y lo envía a DeleteProductCommandHandler
        await _mediator.Send(new DeleteProductCommand(id));
        // Devuelve HTTP 204 No Content (estándar REST cuando se elimina un recurso y no hay cuerpo que devolver)
        return NoContent();
    }
}
