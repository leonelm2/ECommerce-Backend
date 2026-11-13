using MediatR;

namespace ECommerce.Application.Commands.Products;

public sealed record DeleteProductCommand(int Id) : IRequest;
