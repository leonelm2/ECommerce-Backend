using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Commands.Products;

public sealed record UpdateProductCommand(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId) : IRequest<ProductDto>;
