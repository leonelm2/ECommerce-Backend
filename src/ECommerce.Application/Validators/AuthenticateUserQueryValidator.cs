using ECommerce.Application.Queries.Users;
using FluentValidation;

namespace ECommerce.Application.Validators;

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
