using EMIS.SharedKernel;
using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Value Object: Subdomain của tenant (school-abc, school-xyz)
/// Business Rules:
/// - Chỉ chứa chữ thường, số, dấu gạch ngang
/// - Độ dài 3-50 ký tự
/// - Không bắt đầu/kết thúc bằng dấu gạch ngang
/// - Không chứa dấu gạch ngang liên tiếp
/// </summary>
public class Subdomain : ValueObject
{
    private static readonly Regex ValidationPattern = new Regex(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public string Value { get; private set; } = string.Empty;

    private Subdomain() { } // For EF Core

    private Subdomain(string value)
    {
        Value = value;
    }

    public static Subdomain Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Subdomain cannot be empty", nameof(value));

        value = value.Trim().ToLowerInvariant();

        if (value.Length < 3 || value.Length > 50)
            throw new ArgumentException("Subdomain must be between 3 and 50 characters", nameof(value));

        if (!ValidationPattern.IsMatch(value))
            throw new ArgumentException(
                "Subdomain can only contain lowercase letters, numbers, and hyphens. " +
                "Cannot start/end with hyphen or contain consecutive hyphens",
                nameof(value));

        // Reserved subdomains
        var reservedSubdomains = new[] { "admin", "api", "www", "mail", "ftp", "localhost", "app", "portal" };
        if (reservedSubdomains.Contains(value))
            throw new ArgumentException($"Subdomain '{value}' is reserved", nameof(value));

        return new Subdomain(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
