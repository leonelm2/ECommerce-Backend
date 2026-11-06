using ECommerce.Application.Commands.Orders; // Importa CreateOrderCommand
using FluentValidation; // Importa AbstractValidator para definir reglas de validación declarativas

namespace ECommerce.Application.Validators; // Capa Application: validaciones de formato/entrada de Commands y Queries

// Validador de FluentValidation para el comando CreateOrderCommand.
// Se ejecuta automáticamente dentro de ValidationBehavior ANTES de entrar a CreateOrderCommandHandler.
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        // Regla 1: El ID del usuario comprador debe ser un número positivo mayor a 0
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId es obligatorio.");

        // Regla 2: La lista de ítems no puede ser null ni venir vacía []
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Los items son obligatorios.")
            .NotEmpty().WithMessage("La orden debe contener al menos un item.");

        // Regla 3: Recorre cada ítem dentro de la lista "Items" y valida sus campos individuales
        RuleForEach(x => x.Items).ChildRules(items =>
        {
            // El ID del producto debe ser mayor a 0
            items.RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId es obligatorio.");

            // La cantidad a comprar debe ser entre 1 y 100 unidades como máximo
            items.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor que 0.")
                .LessThanOrEqualTo(100).WithMessage("La cantidad no puede ser mayor que 100.");
        });
    }
}
