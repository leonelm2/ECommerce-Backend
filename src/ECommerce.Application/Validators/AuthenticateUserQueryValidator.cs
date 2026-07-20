using ECommerce.Application.Queries.Users;
using FluentValidation;

namespace ECommerce.Application.Validators;

/// <summary>
/// Validador para AuthenticateUserQuery (ahora en namespace Queries.Users).
/// </summary>
public sealed class AuthenticateUserQueryValidator : AbstractValidator<AuthenticateUserQuery>
{
    public AuthenticateUserQueryValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username es obligatorio.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password es obligatorio.");
    }
}
