using MediatR;
using PaymentService.Api.Core.Application.DTOs;

namespace PaymentService.Api.Core.Application.Commands;

public record ProcessPaymentCommand(PaymentRequestDto Request) : IRequest<PaymentResponseDto>;
