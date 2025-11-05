using EMIS.SharedKernel;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Value Object: Số điện thoại (username cho đăng nhập)
/// </summary>
public class PhoneNumber : ValueObject
{
    public string Value { get; private set; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method với validation
    /// </summary>
    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        // Remove spaces and special characters
        var cleanedPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Vietnamese phone validation: 10 digits, starts with 0
        if (cleanedPhone.Length != 10 || !cleanedPhone.StartsWith("0"))
            throw new ArgumentException("Phone number must be 10 digits and start with 0", nameof(phoneNumber));

        return new PhoneNumber(cleanedPhone);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
