using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Infrastructure.Services;

public sealed class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PaymentServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/payments/process")
        {
            Content = JsonContent.Create(request)
        };

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            if (AuthenticationHeaderValue.TryParse(authHeader, out var authHeaderValue))
            {
                httpRequest.Headers.Authorization = authHeaderValue;
            }
        }

        try
        {
            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new AuthenticationException("Acceso no autorizado al servicio de pagos. Token JWT inválido o expirado.");
            }

            if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                var problem = await httpResponse.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(cancellationToken: cancellationToken);
                throw new DomainRuleException($"Datos de pago inválidos (400 Bad Request): {problem?.Detail ?? "Error de validación sintáctica."}");
            }

            if (httpResponse.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                var problem = await httpResponse.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(cancellationToken: cancellationToken);
                throw new DomainRuleException($"Fallo de reglas de negocio en el pago (422 Unprocessable): {problem?.Detail}");
            }

            if (httpResponse.IsSuccessStatusCode)
            {
                var paymentResult = await httpResponse.Content.ReadFromJsonAsync<PaymentResponseDto>(cancellationToken: cancellationToken);
                return paymentResult ?? throw new DomainRuleException("Respuesta de pago vacía recibida del servidor.");
            }

            throw new DomainRuleException($"Error inesperado del servicio de pagos. HTTP: {httpResponse.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            throw new DomainRuleException($"Error de comunicación con el servicio de pagos: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            throw new DomainRuleException("Timeout al comunicarse con el servicio de pagos.");
        }
    }
}
