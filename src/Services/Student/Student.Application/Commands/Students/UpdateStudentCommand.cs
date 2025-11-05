using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Student.Application.DTOs;

namespace Student.Application.Commands.Students;

/// <summary>
/// Command to update student information
/// </summary>
public class UpdateStudentCommand : IRequest<ApiResponse<StudentDetailDto>>
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? EthnicGroup { get; set; }
    public AddressDto? Address { get; set; }
    public string? HealthNotes { get; set; }
    public string? Allergies { get; set; }
    public Guid? ClassId { get; set; }
}
