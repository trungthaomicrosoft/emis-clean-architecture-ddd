using FluentValidation;
using Teacher.Application.Commands;

namespace Teacher.Application.Validators;

public class UpdateTeacherCommandValidator : AbstractValidator<UpdateTeacherCommand>
{
    public UpdateTeacherCommandValidator()
    {
        RuleFor(x => x.TeacherId)
            .NotEmpty().WithMessage("TeacherId is required");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(255).WithMessage("Full name cannot exceed 255 characters");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.Now.AddYears(-18))
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Teacher must be at least 18 years old");
    }
}
