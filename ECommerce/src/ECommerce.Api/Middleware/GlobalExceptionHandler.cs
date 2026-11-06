using ECommerce.Domain.Exceptions; // Importa nuestras excepciones personalizadas de Dominio (NotFoundException, DomainException, AuthenticationException)
using FluentValidation; // Importa ValidationException (la que lanza nuestro ValidationBehavior cuando falla una regla de entrada)
using Microsoft.AspNetCore.Diagnostics; // Importa la interfaz IExceptionHandler de .NET 8
using Microsoft.AspNetCore.Http; // Importa HttpContext y StatusCodes (400, 401, 404, 422, 500)
using Microsoft.AspNetCore.Mvc; // Importa ProblemDetails y ValidationProblemDetails (estándar RFC 7807)
using Microsoft.Extensions.Logging; // Importa ILogger para registrar errores en consola
using System; // Tipos base (Exception, DateTime)
using System.Linq; // Métodos LINQ (GroupBy, ToDictionary, Select) para agrupar errores de validación
using System.Threading; // CancellationToken
using System.Threading.Tasks; // ValueTask

namespace ECommerce.Api.Middleware; // Vive en la capa Api porque traduce excepciones de negocio a códigos de estado HTTP

// Implementa IExceptionHandler (el estándar moderno de .NET 8 para manejo centralizado de errores).
// Gracias a esta clase, ningún Controller necesita tener bloques try/catch.
public class GlobalExceptionHandler : IExceptionHandler
{
    // Logger para dejar registro interno del error en la consola del servidor
    private readonly ILogger<GlobalExceptionHandler> _logger;

    // Constructor: recibe el logger por Inyección de Dependencias
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    // Método que .NET ejecuta automáticamente cada vez que cualquier capa lanza una excepción no controlada
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, // El contexto de la petición HTTP que falló
        Exception exception, // La excepción que fue lanzada (en un Validator, Handler, Entidad o Repositorio)
        CancellationToken cancellationToken)
    {
        // Por defecto, asumimos código 500 (Error Interno del Servidor) por si es un error inesperado
        var statusCode = StatusCodes.Status500InternalServerError;
        // Objeto estándar RFC 7807 que devolveremos como JSON al cliente (tiene Title, Detail, Status, Instance)
        ProblemDetails problemDetails;

        // Evaluamos qué tipo exacto de excepción ocurrió usando Pattern Matching (switch)
        switch (exception)
        {
            // CASO 1: No se encontró un recurso en la BD (ej. Usuario o Producto inexistente) -> Devuelve 404 Not Found
            case NotFoundException notFound:
                statusCode = StatusCodes.Status404NotFound;
                problemDetails = new ProblemDetails
                {
                    Title = "Recurso no encontrado", // Título legible del error
                    Detail = notFound.Message, // Mensaje específico (ej. "Producto con id 99 no encontrado.")
                    Status = statusCode, // 404
                    Instance = httpContext.Request.Path // La ruta URL donde ocurrió el error (ej. "/api/orders")
                };
                break;

            // CASO 2: Falló FluentValidation en el ValidationBehavior (datos de entrada mal formados) -> Devuelve 400 Bad Request
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                // Agrupa los errores por nombre de propiedad (ej. "Quantity": ["La cantidad debe ser mayor que 0."])
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                // ValidationProblemDetails extiende ProblemDetails agregando el diccionario "errors" con cada campo inválido
                problemDetails = new ValidationProblemDetails(errors)
                {
                    Title = "Error de validación",
                    Detail = "Uno o más campos tienen valores no válidos.",
                    Status = statusCode, // 400
                    Instance = httpContext.Request.Path
                };
                break;

            // CASO 3: Credenciales inválidas en login o token rechazado por PaymentService -> Devuelve 401 Unauthorized
            case AuthenticationException authenticationException:
                statusCode = StatusCodes.Status401Unauthorized;
                problemDetails = new ProblemDetails
                {
                    Title = "No autorizado",
                    Detail = authenticationException.Message,
                    Status = statusCode, // 401
                    Instance = httpContext.Request.Path
                };
                break;

            // CASO 4: Violación de Regla de Negocio/Dominio (incluye DomainRuleException e InsufficientStockException) -> Devuelve 422 Unprocessable Entity
            // Nota clave de examen: 400 es error sintáctico (formato); 422 es error semántico (el JSON está bien, pero no hay stock o el pago fue rechazado)
            case DomainException domainException:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                problemDetails = new ProblemDetails
                {
                    Title = "Regla de dominio inválida",
                    Detail = domainException.Message,
                    Status = statusCode, // 422
                    Instance = httpContext.Request.Path
                };
                break;

            // CASO 5: Cualquier otro error inesperado (bug de código, caída de base de datos, NullReferenceException) -> Devuelve 500
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                problemDetails = new ProblemDetails
                {
                    Title = "Error interno del servidor",
                    // Por seguridad, NUNCA exponemos el stack trace ni detalles técnicos internos al cliente en un error 500
                    Detail = "Ocurrió un error al procesar la solicitud.",
                    Status = statusCode, // 500
                    Instance = httpContext.Request.Path
                };
                break;
        }

        // Registra el error completo (con su StackTrace) en los logs de la consola del servidor para que el desarrollador pueda depurarlo
        _logger.LogError(exception, "Error procesando la solicitud en {Path}", httpContext.Request.Path);
        // Además guarda el detalle técnico en un archivo local "error.log" con fecha y hora UTC
        System.IO.File.AppendAllText("error.log", $"{DateTime.UtcNow}: {exception.ToString()}\n");

        // Configura el header Content-Type oficial del estándar RFC 7807
        httpContext.Response.ContentType = "application/problem+json";
        // Asigna el código de estado HTTP correspondiente (400, 401, 404, 422 o 500)
        httpContext.Response.StatusCode = statusCode;
        // Serializa el objeto problemDetails a JSON y lo escribe en la respuesta HTTP hacia el cliente
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Devuelve "true" para indicarle a ASP.NET Core que la excepción ya fue completamente manejada y no debe detener el servidor
        return true;
    }
}
