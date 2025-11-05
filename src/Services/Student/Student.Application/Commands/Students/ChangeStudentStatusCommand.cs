using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Student.Application.Commands.Students;

/// <summary>
/// Command to change student status
/// </summary>
public class ChangeStudentStatusCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public int Status { get; set; }

    public ChangeStudentStatusCommand(Guid id, int status)
    {
        Id = id;
        Status = status;
    }
}
