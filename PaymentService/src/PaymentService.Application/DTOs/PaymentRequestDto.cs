namespace PaymentService.Application.DTOs;

public sealed record PaymentRequestDto(
    int OrderId,
    int UserId,
    decimal Amount,
    string Currency,
    string? Description
);
