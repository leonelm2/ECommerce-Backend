using FluentValidation; // Importa IValidator, ValidationContext y ValidationException
using MediatR; // Importa IPipelineBehavior y RequestHandlerDelegate

namespace ECommerce.Application.Behaviors; // Vive en Application porque intercepta los Commands/Queries antes de que lleguen a los Handlers

// Implementa IPipelineBehavior de MediatR: funciona como un "peaje" o embudo por el que pasan TODOS los Commands y Queries
// antes de llegar a su Handler correspondiente.
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> // Restricción genérica: aplica a cualquier Request de MediatR
{
    // Colección de todos los validadores de FluentValidation que existan para este tipo de comando (TRequest)
    // Ejemplo: Si TRequest es CreateOrderCommand, acá se inyecta automáticamente CreateOrderCommandValidator.
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    // Constructor: recibe los validadores registrados en Program.cs
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    // Método interceptor que se ejecuta cuando alguien llama a "_mediator.Send(request)"
    public async Task<TResponse> Handle(
        TRequest request, // El Command o Query que envió el Controller (ej. CreateOrderCommand)
        RequestHandlerDelegate<TResponse> next, // Puntero/delegado al siguiente paso de la cadena (el Handler real)
        CancellationToken cancellationToken)
    {
        // 1. Si este Command o Query no tiene ningún Validator creado (por ejemplo, GetAllProductsQuery),
        // simplemente llama a "await next()" para que siga directo hacia el Handler sin hacer nada acá.
        if (!_validators.Any())
        {
            return await next();
        }

        // 2. Si hay validadores, envuelve el Command/Query en un contexto de validación de FluentValidation
        var context = new ValidationContext<TRequest>(request);
        // 3. Ejecuta todos los validadores de ese Command en paralelo (ValidateAsync)
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // 4. Junta todos los errores encontrados (si hubo alguno) en una sola lista de fallos
        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .ToList();

        // 5. ¡PUNTO CLAVE DE EXAMEN! Si hubo al menos un error de validación sintáctica (ej. Quantity <= 0 o Email vacío),
        // lanza ValidationException ACÁ MISMO. El Handler NUNCA se llega a ejecutar ni se toca la base de datos.
        // Esa ValidationException sube hasta GlobalExceptionHandler, que la transforma en un HTTP 400 Bad Request.
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        // 6. Si todas las reglas de FluentValidation pasaron sin errores, deja pasar el Command hacia el Handler real
        return await next();
    }
}
