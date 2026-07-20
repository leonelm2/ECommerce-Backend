namespace ECommerce.Application.DTOs;

/// <summary>
/// DTO de solicitud de pago para enviar al PaymentService.
/// </summary>
public sealed record PaymentRequestDto(
    int OrderId,
    int UserId,
    decimal Amount,
    string Currency,
    string? Description
);
