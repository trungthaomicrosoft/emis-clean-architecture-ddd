using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Teacher.Application.DTOs;

namespace Teacher.Application.Commands;

/// <summary>
/// Command: Tạo giáo viên mới
/// </summary>
public class CreateTeacherCommand : IRequest<ApiResponse<TeacherDetailDto>>
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public AddressDto? Address { get; set; }
    public DateTime? HireDate { get; set; }
}
