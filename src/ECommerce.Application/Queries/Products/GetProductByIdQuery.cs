using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Queries.Products;

/// <summary>
/// Query para obtener un producto por ID.
/// Movida de Commands/ a Queries/ para respetar la separación CQRS correctamente.
/// </summary>
public sealed record GetProductByIdQuery(int Id) : IRequest<ProductDto>;
