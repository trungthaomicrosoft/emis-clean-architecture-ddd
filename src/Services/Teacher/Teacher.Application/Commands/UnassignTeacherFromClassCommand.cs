using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Teacher.Application.Commands;

/// <summary>
/// Command: Gỡ phân công giáo viên khỏi lớp
/// </summary>
public class UnassignTeacherFromClassCommand : IRequest<ApiResponse<bool>>
{
    public Guid TeacherId { get; set; }
    public Guid ClassId { get; set; }
    public DateTime? EndDate { get; set; }
}
