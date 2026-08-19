using System.Threading;
using System.Threading.Tasks;
using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface IPaymentServiceClient
{
    Task<PaymentResponseDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default);
}
