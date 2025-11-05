using EMIS.SharedKernel;
using System.Text.RegularExpressions;

namespace Student.Domain.ValueObjects;

/// <summary>
/// Value Object: Thông tin liên hệ
/// </summary>
public class ContactInfo : ValueObject
{
    public string PhoneNumber { get; private set; }
    public string? Email { get; private set; }

    private ContactInfo() { } // For EF Core

    private ContactInfo(string phoneNumber, string? email)
    {
        PhoneNumber = phoneNumber;
        Email = email;
    }

    public static ContactInfo Create(string phoneNumber, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        phoneNumber = phoneNumber.Trim();

        if (!IsValidPhoneNumber(phoneNumber))
            throw new ArgumentException($"Invalid phone number format: {phoneNumber}", nameof(phoneNumber));

        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim().ToLower();
            if (!IsValidEmail(email))
                throw new ArgumentException($"Invalid email format: {email}", nameof(email));
        }

        return new ContactInfo(phoneNumber, email);
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        // Vietnamese phone number format: 10 digits starting with 0
        // Example: 0123456789
        var regex = new Regex(@"^0\d{9}$");
        return regex.IsMatch(phoneNumber);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PhoneNumber;
        if (Email != null)
            yield return Email;
    }

    public override string ToString() => 
        Email != null ? $"{PhoneNumber} ({Email})" : PhoneNumber;
}
