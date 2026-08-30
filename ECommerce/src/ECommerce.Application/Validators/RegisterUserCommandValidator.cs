using ECommerce.Application.Commands.Users;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username es obligatorio.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email es obligatorio.")
            .EmailAddress().WithMessage("Email no es válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password es obligatorio.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un número.");
    }
}
