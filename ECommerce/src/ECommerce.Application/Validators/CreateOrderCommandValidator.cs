using ECommerce.Application.Commands.Orders;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId es obligatorio.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Los items son obligatorios.")
            .NotEmpty().WithMessage("La orden debe contener al menos un item.");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId es obligatorio.");

            items.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor que 0.")
                .LessThanOrEqualTo(100).WithMessage("La cantidad no puede ser mayor que 100.");
        });
    }
}
