using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Commands.Products;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId) : IRequest<Product>;
