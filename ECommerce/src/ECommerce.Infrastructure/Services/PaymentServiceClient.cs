using System; // Importa tipos base de .NET (como Uri, ArgumentNullException, Guid)
using System.Net; // Importa HttpStatusCode (para comparar códigos 400, 401, 422, etc.)
using System.Net.Http; // Importa HttpClient, HttpRequestMessage, HttpMethod
using System.Net.Http.Headers; // Importa AuthenticationHeaderValue (para reenviar el header Authorization)
using System.Net.Http.Json; // Importa JsonContent y ReadFromJsonAsync (para serializar/deserializar JSON fácilmente)
using System.Threading; // Importa CancellationToken (para cancelar peticiones si el usuario corta la conexión)
using System.Threading.Tasks; // Importa Task<T> para programación asíncrona (async/await)
using ECommerce.Application.DTOs; // Importa PaymentRequestDto y PaymentResponseDto (los contratos de datos)
using ECommerce.Application.Interfaces; // Importa la interfaz IPaymentServiceClient que esta clase implementa
using ECommerce.Domain.Exceptions; // Importa AuthenticationException y DomainRuleException
using Microsoft.AspNetCore.Http; // Importa IHttpContextAccessor para poder leer la petición HTTP actual

namespace ECommerce.Infrastructure.Services; // Pertenece a la capa Infrastructure porque se comunica con un servicio externo por HTTP

// "sealed" significa que ninguna otra clase puede heredar de PaymentServiceClient.
// Implementa IPaymentServiceClient (definida en Application) aplicando el Principio de Inversión de Dependencias (SOLID).
public sealed class PaymentServiceClient : IPaymentServiceClient
{
    // Cliente HTTP inyectado y administrado por IHttpClientFactory (evita el agotamiento de sockets / Socket Exhaustion)
    private readonly HttpClient _httpClient;
    // Accesor que nos permite leer el HttpContext de la petición original que hizo el cliente a nuestra API
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Constructor: recibe las dependencias por Inyección de Dependencias (configuradas en InfrastructureServiceExtensions.cs)
    public PaymentServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        // Guarda el HttpClient o lanza excepción si viniera nulo (Guard Clause)
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        // Guarda el IHttpContextAccessor o lanza excepción si viniera nulo
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    // Método asíncrono que envía la solicitud de cobro al microservicio PaymentService y devuelve un PaymentResponseDto
    public async Task<PaymentResponseDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Crea el mensaje HTTP POST apuntando a la ruta relativa "api/payments/process"
        // (la URL base http://localhost:5200 ya fue configurada en InfrastructureServiceExtensions.cs)
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/payments/process")
        {
            // Serializa automáticamente el DTO "request" a formato JSON y pone el header Content-Type: application/json
            Content = JsonContent.Create(request)
        };

        // 2. PROPAGACIÓN DEL TOKEN JWT: Obtenemos el contexto HTTP de la petición entrante actual
        var httpContext = _httpContextAccessor.HttpContext;
        // Si hay un contexto activo y la petición original trajo un header llamado "Authorization"
        if (httpContext != null && httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            // Intenta parsear el texto del header (ej: "Bearer eyJhbGci...") a un objeto AuthenticationHeaderValue válido
            if (AuthenticationHeaderValue.TryParse(authHeader, out var authHeaderValue))
            {
                // Adjunta ese mismo token JWT a la petición saliente hacia el microservicio de pagos
                httpRequest.Headers.Authorization = authHeaderValue;
            }
        }

        // 3. Envía la petición HTTP de forma asíncrona.
        // NOTA CLAVE: Acá actúa implícitamente la política de Polly (3 reintentos con Exponential Backoff si hay error de red o 5xx).
        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

        // 4. MANEJO DE CÓDIGOS DE ESTADO HTTP DE RESPUESTA:
        // Si el microservicio de pagos respondió 401 Unauthorized (token inválido o vencido)
        if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Lanza nuestra excepción de dominio que GlobalExceptionHandler traducirá a un 401 para el cliente
            throw new AuthenticationException("Acceso no autorizado al servicio de pagos. Token JWT inválido o expirado.");
        }

        // Si el microservicio de pagos respondió 400 Bad Request (por ejemplo, monto <= 0)
        if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            // Deserializa el cuerpo del error en formato estándar RFC 7807 (ProblemDetails)
            var problem = await httpResponse.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(cancellationToken: cancellationToken);
            // Lanza DomainRuleException con el detalle del error
            throw new DomainRuleException($"Datos de pago inválidos (400 Bad Request): {problem?.Detail ?? "Error de validación sintáctica."}");
        }

        // Si el microservicio de pagos respondió 422 Unprocessable Entity (violación de regla de negocio en el pago)
        if (httpResponse.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            // Lee el ProblemDetails devuelto por el servicio de pagos
            var problem = await httpResponse.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>(cancellationToken: cancellationToken);
            // Lanza DomainRuleException para que el Handler compense la orden y devuelva el stock
            throw new DomainRuleException($"Fallo de reglas de negocio en el pago (422 Unprocessable): {problem?.Detail}");
        }

        // Si el código HTTP es exitoso (rango 200-299, por ejemplo 200 OK)
        if (httpResponse.IsSuccessStatusCode)
        {
            // Deserializa el JSON de respuesta del microservicio a nuestro DTO PaymentResponseDto
            var paymentResult = await httpResponse.Content.ReadFromJsonAsync<PaymentResponseDto>(cancellationToken: cancellationToken);
            // Devuelve el DTO al Handler (o lanza excepción si por algún motivo el JSON vino vacío/null)
            return paymentResult ?? throw new DomainRuleException("Respuesta de pago vacía recibida del servidor.");
        }

        // Si respondió cualquier otro código inesperado (ej. 500 Internal Server Error luego de agotar los reintentos de Polly)
        throw new DomainRuleException($"Error inesperado del servicio de pagos. HTTP: {httpResponse.StatusCode}");
    }
}
