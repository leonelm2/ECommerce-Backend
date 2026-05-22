using ECommerce.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = StatusCodes.Status500InternalServerError;
        ProblemDetails problemDetails;

        switch (exception)
        {
            case NotFoundException notFound:
                statusCode = StatusCodes.Status404NotFound;
                problemDetails = new ProblemDetails
                {
                    Title = "Recurso no encontrado",
                    Detail = notFound.Message,
                    Status = statusCode,
                    Instance = httpContext.Request.Path
                };
                break;

            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                problemDetails = new ValidationProblemDetails(errors)
                {
                    Title = "Error de validación",
                    Detail = "Uno o más campos tienen valores no válidos.",
                    Status = statusCode,
                    Instance = httpContext.Request.Path
                };
                break;

            case AuthenticationException authenticationException:
                statusCode = StatusCodes.Status401Unauthorized;
                problemDetails = new ProblemDetails
                {
                    Title = "No autorizado",
                    Detail = authenticationException.Message,
                    Status = statusCode,
                    Instance = httpContext.Request.Path
                };
                break;

            case DomainException domainException:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                problemDetails = new ProblemDetails
                {
                    Title = "Regla de dominio inválida",
                    Detail = domainException.Message,
                    Status = statusCode,
                    Instance = httpContext.Request.Path
                };
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                problemDetails = new ProblemDetails
                {
                    Title = "Error interno del servidor",
                    Detail = "Ocurrió un error al procesar la solicitud.",
                    Status = statusCode,
                    Instance = httpContext.Request.Path
                };
                break;
        }

        _logger.LogError(exception, "Error procesando la solicitud en {Path}", httpContext.Request.Path);

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
