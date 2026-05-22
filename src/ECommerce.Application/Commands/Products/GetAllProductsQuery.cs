using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Commands.Products;

public sealed record GetAllProductsQuery() : IRequest<IEnumerable<Product>>;
