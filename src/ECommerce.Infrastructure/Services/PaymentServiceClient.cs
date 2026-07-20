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

/// <summary>
/// Cliente HTTP para comunicarse con el microservicio PaymentService.
/// Utiliza HttpClient inyectado por IHttpClientFactory y HttpContextAccessor
/// para reenviar el token JWT del usuario autenticado.
/// </summary>
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

        // 1. Obtener y reenviar el token JWT del usuario autenticado actual
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
            // 2. Realizar llamada con timeout implícito (configurado en el HttpClient)
            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            // 3. Manejar códigos HTTP específicos de error del sistema
            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new AuthenticationException("Acceso no autorizado al servicio de pagos. Token JWT inválido o expirado.");
            }

            if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                // FluentValidation falló en el microservicio
                var problem = await httpResponse.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(cancellationToken: cancellationToken);
                throw new DomainRuleException($"Datos de pago inválidos (400 Bad Request): {problem?.Detail ?? "Error de validación sintáctica."}");
            }

            if (httpResponse.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                // Regla de dominio rota en el microservicio
                var problem = await httpResponse.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(cancellationToken: cancellationToken);
                throw new DomainRuleException($"Fallo de reglas de negocio en el pago (422 Unprocessable): {problem?.Detail}");
            }

            // 4. Deserializar respuesta exitosa (200 OK para rechazado, 201 Created para aprobado)
            if (httpResponse.IsSuccessStatusCode)
            {
                var paymentResult = await httpResponse.Content.ReadFromJsonAsync<PaymentResponseDto>(cancellationToken: cancellationToken);
                return paymentResult ?? throw new DomainRuleException("Respuesta de pago vacía recibida del servidor.");
            }

            // Cualquier otro código de error HTTP (500, 502, 503, 504, etc.)
            throw new DomainRuleException($"Error inesperado del servicio de pagos. Código de estado HTTP: {httpResponse.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            // Errores de conexión física (servidor apagado, caída de red, DNS)
            throw new DomainRuleException($"Error de comunicación con el servicio de pagos. El servidor podría estar inaccesible: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            // Timeout de la solicitud
            throw new DomainRuleException("Se excedió el tiempo de espera (Timeout) al comunicarse con el servicio de pagos.");
        }
    }
}
