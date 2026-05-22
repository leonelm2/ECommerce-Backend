using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Commands.Orders;

public sealed record GetOrderByIdQuery(int Id) : IRequest<Order>;
