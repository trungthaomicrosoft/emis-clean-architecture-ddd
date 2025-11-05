using FluentValidation;
using Identity.Application.Commands;

namespace Identity.Application.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^0\d{9}$").WithMessage("Phone number must be 10 digits and start with 0");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
