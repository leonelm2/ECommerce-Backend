using ECommerce.Application.DTOs;
using MediatR;

namespace ECommerce.Application.Queries.Orders;

/// <summary>
/// Query para obtener una orden por ID.
/// Movida de Commands/ a Queries/ para respetar la separación CQRS correctamente.
/// </summary>
public sealed record GetOrderByIdQuery(int Id) : IRequest<OrderDto>;
