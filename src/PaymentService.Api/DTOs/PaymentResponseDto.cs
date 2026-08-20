using System;

namespace PaymentService.Api.DTOs;

public sealed record PaymentResponseDto(
    Guid PaymentId,
    int OrderId,
    int UserId,
    string Status,
    string TransactionCode,
    decimal Amount,
    string Currency,
    DateTime ProcessedAt,
    string Message
);
