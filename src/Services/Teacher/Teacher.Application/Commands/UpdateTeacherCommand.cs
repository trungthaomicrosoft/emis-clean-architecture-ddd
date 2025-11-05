using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Teacher.Application.DTOs;

namespace Teacher.Application.Commands;

/// <summary>
/// Command: Cập nhật thông tin giáo viên
/// </summary>
public class UpdateTeacherCommand : IRequest<ApiResponse<TeacherDetailDto>>
{
    public Guid TeacherId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public AddressDto? Address { get; set; }
}
