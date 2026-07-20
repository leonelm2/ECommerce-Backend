using System.Threading;
using System.Threading.Tasks;
using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Abstracción para el cliente del servicio de pagos.
/// Desacopla la capa de aplicación de la tecnología de comunicación (HTTP/HttpClient).
/// </summary>
public interface IPaymentServiceClient
{
    /// <summary>
    /// Envía una solicitud de pago al servicio de pagos.
    /// </summary>
    /// <param name="request">Datos del pago.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado del pago.</returns>
    Task<PaymentResponseDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default);
}
