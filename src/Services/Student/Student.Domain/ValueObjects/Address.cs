using EMIS.SharedKernel;

namespace Student.Domain.ValueObjects;

/// <summary>
/// Value Object: Địa chỉ
/// </summary>
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string Ward { get; private set; }
    public string District { get; private set; }
    public string City { get; private set; }
    public string? PostalCode { get; private set; }

    private Address() { } // For EF Core

    private Address(string street, string ward, string district, string city, string? postalCode)
    {
        Street = street;
        Ward = ward;
        District = district;
        City = city;
        PostalCode = postalCode;
    }

    public static Address Create(string street, string ward, string district, string city, string? postalCode = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty", nameof(street));

        if (string.IsNullOrWhiteSpace(ward))
            throw new ArgumentException("Ward cannot be empty", nameof(ward));

        if (string.IsNullOrWhiteSpace(district))
            throw new ArgumentException("District cannot be empty", nameof(district));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        return new Address(street, ward, district, city, postalCode);
    }

    public string GetFullAddress()
    {
        return $"{Street}, {Ward}, {District}, {City}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return Ward;
        yield return District;
        yield return City;
        if (PostalCode != null)
            yield return PostalCode;
    }

    public override string ToString() => GetFullAddress();
}
