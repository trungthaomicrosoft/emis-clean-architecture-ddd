namespace Student.Application.DTOs;

/// <summary>
/// DTO for Student basic information
/// </summary>
public class StudentDto
{
    public Guid Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string GenderName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? EthnicGroup { get; set; }
    public AddressDto? Address { get; set; }
    public string? Avatar { get; set; }
    public string? HealthNotes { get; set; }
    public string? Allergies { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public DateTime? GraduationDate { get; set; }
    public Guid? ClassId { get; set; }
    public string? ClassName { get; set; }
    public Guid TenantId { get; set; }
}

/// <summary>
/// DTO for detailed Student information with parents
/// </summary>
public class StudentDetailDto : StudentDto
{
    public List<ParentDto> Parents { get; set; } = new();
}

/// <summary>
/// DTO for Address value object
/// </summary>
public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string FullAddress { get; set; } = string.Empty;
}

/// <summary>
/// DTO for Parent information
/// </summary>
public class ParentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string GenderName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int Relation { get; set; }
    public string RelationName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public AddressDto? Address { get; set; }
    public string? Job { get; set; }
    public string? Workplace { get; set; }
    public bool IsPrimaryContact { get; set; }
}

/// <summary>
/// DTO for Class information
/// </summary>
public class ClassDto
{
    public Guid Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public int? Capacity { get; set; }
    public Guid? MainTeacherId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int StudentCount { get; set; }
}
