using EMIS.SharedKernel;

namespace Student.Domain.ValueObjects;

/// <summary>
/// Value Object: Mã học sinh
/// Format: HS + Year + Sequential (e.g., HS2025001)
/// </summary>
public class StudentCode : ValueObject
{
    public string Value { get; private set; }

    private StudentCode() { } // For EF Core

    private StudentCode(string value)
    {
        Value = value;
    }

    public static StudentCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Student code cannot be empty", nameof(value));

        value = value.Trim().ToUpper();

        if (!IsValid(value))
            throw new ArgumentException($"Invalid student code format: {value}", nameof(value));

        return new StudentCode(value);
    }

    public static StudentCode Generate(int year, int sequence)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000 and 2100", nameof(year));

        if (sequence < 1 || sequence > 999999)
            throw new ArgumentException("Sequence must be between 1 and 999999", nameof(sequence));

        var value = $"HS{year}{sequence:D6}";
        return new StudentCode(value);
    }

    private static bool IsValid(string value)
    {
        // Format: HS + 4 digits (year) + 6 digits (sequence)
        // Example: HS2025000001
        if (value.Length != 12)
            return false;

        if (!value.StartsWith("HS"))
            return false;

        return value.Substring(2).All(char.IsDigit);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(StudentCode code) => code.Value;
}
