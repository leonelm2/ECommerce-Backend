using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Queries.Products;

public sealed record GetProductByIdQuery(int Id) : IRequest<ProductDto>;
