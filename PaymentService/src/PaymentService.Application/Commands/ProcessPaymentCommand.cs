using MediatR;
using PaymentService.Application.DTOs;

namespace PaymentService.Application.Commands;

public record ProcessPaymentCommand(PaymentRequestDto Request) : IRequest<PaymentResponseDto>;
