using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Student.Application.DTOs;

namespace Student.Application.Commands.Students;

/// <summary>
/// Command to create a new student
/// </summary>
public class CreateStudentCommand : IRequest<ApiResponse<StudentDetailDto>>
{
    public string FullName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? EthnicGroup { get; set; }
    public AddressDto? Address { get; set; }
    public string? HealthNotes { get; set; }
    public string? Allergies { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public Guid? ClassId { get; set; }
    
    // Parents information
    public List<CreateParentDto> Parents { get; set; } = new();
}

public class CreateParentDto
{
    public string FullName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int Relation { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public AddressDto? Address { get; set; }
    public string? Job { get; set; }
    public string? Workplace { get; set; }
    public bool IsPrimaryContact { get; set; }
}
