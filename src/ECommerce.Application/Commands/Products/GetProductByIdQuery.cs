using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Commands.Products;

public sealed record GetProductByIdQuery(int Id) : IRequest<Product>;
