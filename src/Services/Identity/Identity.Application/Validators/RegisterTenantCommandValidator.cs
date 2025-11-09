using FluentValidation;
using Identity.Application.Commands;

namespace Identity.Application.Validators;

/// <summary>
/// Validator cho RegisterTenantCommand
/// Validates tenant info và admin credentials
/// </summary>
public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        // Tenant validation
        RuleFor(x => x.SchoolName)
            .NotEmpty().WithMessage("School name is required")
            .MinimumLength(3).WithMessage("School name must be at least 3 characters")
            .MaximumLength(255).WithMessage("School name cannot exceed 255 characters");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required")
            .MinimumLength(3).WithMessage("Subdomain must be at least 3 characters")
            .MaximumLength(50).WithMessage("Subdomain cannot exceed 50 characters")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Subdomain can only contain lowercase letters, numbers, and hyphens. Cannot start/end with hyphen.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Contact phone is required")
            .Matches(@"^0\d{9}$").WithMessage("Contact phone must be 10 digits and start with 0");

        RuleFor(x => x.Address)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address))
            .WithMessage("Address cannot exceed 500 characters");

        // Admin validation
        RuleFor(x => x.AdminFullName)
            .NotEmpty().WithMessage("Admin full name is required")
            .MinimumLength(2).WithMessage("Admin full name must be at least 2 characters")
            .MaximumLength(255).WithMessage("Admin full name cannot exceed 255 characters");

        RuleFor(x => x.AdminPhoneNumber)
            .NotEmpty().WithMessage("Admin phone number is required")
            .Matches(@"^0\d{9}$").WithMessage("Admin phone number must be 10 digits and start with 0");

        RuleFor(x => x.AdminEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.AdminEmail))
            .WithMessage("Invalid admin email format");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Admin password is required")
            .MinimumLength(8).WithMessage("Admin password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Admin password cannot exceed 100 characters")
            .Matches(@"[A-Z]").WithMessage("Admin password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Admin password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Admin password must contain at least one number")
            .Matches(@"[\W_]").WithMessage("Admin password must contain at least one special character");
    }
}
