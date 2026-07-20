using System;

namespace ECommerce.Application.DTOs;

/// <summary>
/// DTO de respuesta de pago devuelto por el PaymentService.
/// </summary>
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
