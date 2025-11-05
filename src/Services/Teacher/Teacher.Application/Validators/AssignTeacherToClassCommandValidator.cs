using FluentValidation;
using Teacher.Application.Commands;

namespace Teacher.Application.Validators;

public class AssignTeacherToClassCommandValidator : AbstractValidator<AssignTeacherToClassCommand>
{
    public AssignTeacherToClassCommandValidator()
    {
        RuleFor(x => x.TeacherId)
            .NotEmpty().WithMessage("TeacherId is required");

        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage("ClassId is required");

        RuleFor(x => x.ClassName)
            .NotEmpty().WithMessage("ClassName is required")
            .MaximumLength(100).WithMessage("ClassName cannot exceed 100 characters");

        RuleFor(x => x.Role)
            .InclusiveBetween(1, 3).WithMessage("Role must be 1 (Primary), 2 (Support), or 3 (Substitute)");
    }
}
