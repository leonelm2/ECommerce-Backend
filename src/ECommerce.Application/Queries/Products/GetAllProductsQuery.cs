using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Queries.Products;

/// <summary>
/// Query para obtener todos los productos.
/// Movida de Commands/ a Queries/ para respetar la separación CQRS correctamente.
/// </summary>
public sealed record GetAllProductsQuery() : IRequest<IEnumerable<ProductDto>>;
