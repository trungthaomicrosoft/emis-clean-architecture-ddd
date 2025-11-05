using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Teacher.Application.Commands;

/// <summary>
/// Command: Xóa giáo viên
/// </summary>
public class DeleteTeacherCommand : IRequest<ApiResponse<bool>>
{
    public Guid TeacherId { get; set; }

    public DeleteTeacherCommand(Guid teacherId)
    {
        TeacherId = teacherId;
    }
}
