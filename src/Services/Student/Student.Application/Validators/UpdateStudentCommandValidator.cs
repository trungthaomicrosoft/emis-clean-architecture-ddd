using FluentValidation;
using Student.Application.Commands.Students;

namespace Student.Application.Validators;

/// <summary>
/// Validator for UpdateStudentCommand
/// </summary>
public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID không được để trống");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(200).WithMessage("Họ tên không được vượt quá 200 ký tự");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Giới tính không hợp lệ");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Ngày sinh không được để trống")
            .LessThan(DateTime.UtcNow).WithMessage("Ngày sinh phải trước ngày hiện tại")
            .Must(BeValidAge).WithMessage("Tuổi học sinh phải từ 0-6 tuổi (mầm non)");
    }

    private bool BeValidAge(DateTime dateOfBirth)
    {
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (dateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
        return age >= 0 && age <= 6;
    }
}
