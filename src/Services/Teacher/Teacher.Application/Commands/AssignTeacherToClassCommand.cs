using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Teacher.Application.Commands;

/// <summary>
/// Command: Phân công giáo viên vào lớp
/// </summary>
public class AssignTeacherToClassCommand : IRequest<ApiResponse<bool>>
{
    public Guid TeacherId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int Role { get; set; } // 1=Primary, 2=Support, 3=Substitute
    public DateTime? StartDate { get; set; }
}
