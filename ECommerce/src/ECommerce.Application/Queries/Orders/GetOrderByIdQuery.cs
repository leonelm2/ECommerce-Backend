using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Queries.Orders;

public sealed record GetOrderByIdQuery(int Id) : IRequest<OrderDto>;
