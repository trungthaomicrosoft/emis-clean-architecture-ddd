using FluentValidation;
using Student.Application.Commands.Students;

namespace Student.Application.Validators;

/// <summary>
/// Validator for CreateStudentCommand
/// </summary>
public class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(200).WithMessage("Họ tên không được vượt quá 200 ký tự");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Giới tính không hợp lệ");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Ngày sinh không được để trống")
            .LessThan(DateTime.UtcNow).WithMessage("Ngày sinh phải trước ngày hiện tại")
            .Must(BeValidAge).WithMessage("Tuổi học sinh phải từ 0-6 tuổi (mầm non)");

        RuleFor(x => x.EnrollmentDate)
            .NotEmpty().WithMessage("Ngày nhập học không được để trống")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Ngày nhập học không được sau ngày hiện tại");

        RuleFor(x => x.Parents)
            .NotEmpty().WithMessage("Phải có ít nhất 1 phụ huynh")
            .Must(HaveAtLeastOnePrimaryContact).WithMessage("Phải có ít nhất 1 phụ huynh là người liên hệ chính");

        RuleForEach(x => x.Parents).ChildRules(parent =>
        {
            parent.RuleFor(p => p.FullName)
                .NotEmpty().WithMessage("Họ tên phụ huynh không được để trống")
                .MaximumLength(200).WithMessage("Họ tên phụ huynh không được vượt quá 200 ký tự");

            parent.RuleFor(p => p.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống")
                .Matches(@"^0\d{9}$").WithMessage("Số điện thoại không hợp lệ (phải có 10 số và bắt đầu bằng 0)");

            parent.RuleFor(p => p.Email)
                .EmailAddress().When(p => !string.IsNullOrEmpty(p.Email))
                .WithMessage("Email không hợp lệ");

            parent.RuleFor(p => p.Relation)
                .IsInEnum().WithMessage("Quan hệ không hợp lệ");
        });
    }

    private bool BeValidAge(DateTime dateOfBirth)
    {
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (dateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
        return age >= 0 && age <= 6;
    }

    private bool HaveAtLeastOnePrimaryContact(List<CreateParentDto> parents)
    {
        return parents.Any(p => p.IsPrimaryContact);
    }
}
