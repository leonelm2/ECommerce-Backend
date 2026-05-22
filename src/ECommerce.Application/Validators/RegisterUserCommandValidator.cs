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
            .NotEmpty().WithMessage("Password es obligatorio.");
    }
}
