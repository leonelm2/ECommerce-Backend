using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Queries.Products;

public sealed record GetAllProductsQuery() : IRequest<IEnumerable<ProductDto>>;
