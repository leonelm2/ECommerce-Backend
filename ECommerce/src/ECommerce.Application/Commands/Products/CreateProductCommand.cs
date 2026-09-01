using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Commands.Products;

// PASO 2: EL COMANDO (EL PAQUETE)
// Este record es simplemente un transportador de datos. Trae lo que el usuario escribió (Nombre, Precio, etc).
// Al heredar de IRequest<ProductDto>, le está diciendo a MediatR: "Quien procese este paquete, debe devolver un ProductDto".
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId) : IRequest<ProductDto>;
