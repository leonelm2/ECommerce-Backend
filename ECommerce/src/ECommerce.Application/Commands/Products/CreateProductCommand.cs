using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Commands.Products;

// FLUJO DE REQUEST (PRODUCTS - PASO 2): El Command es el paquete de datos que viaja por MediatR.
// Al implementar IRequest<ProductDto>, le indica a MediatR que su Handler devolverá un DTO de tipo ProductDto.
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId) : IRequest<ProductDto>;
