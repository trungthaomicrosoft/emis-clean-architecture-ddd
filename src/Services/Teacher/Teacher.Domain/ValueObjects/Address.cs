using EMIS.SharedKernel;

namespace Teacher.Domain.ValueObjects;

/// <summary>
/// Value Object: Địa chỉ
/// </summary>
public class Address : ValueObject
{
    public string? Street { get; private set; }
    public string? Ward { get; private set; }
    public string? District { get; private set; }
    public string? City { get; private set; }

    private Address() { } // For EF Core

    private Address(string? street, string? ward, string? district, string? city)
    {
        Street = street;
        Ward = ward;
        District = district;
        City = city;
    }

    public static Address Create(string? street, string? ward, string? district, string? city)
    {
        return new Address(street, ward, district, city);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street ?? string.Empty;
        yield return Ward ?? string.Empty;
        yield return District ?? string.Empty;
        yield return City ?? string.Empty;
    }

    public override string ToString()
    {
        var parts = new[] { Street, Ward, District, City }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}
