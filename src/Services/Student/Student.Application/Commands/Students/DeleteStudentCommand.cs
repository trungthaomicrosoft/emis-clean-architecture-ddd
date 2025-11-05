using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Student.Application.Commands.Students;

/// <summary>
/// Command to delete a student
/// </summary>
public class DeleteStudentCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }

    public DeleteStudentCommand(Guid id)
    {
        Id = id;
    }
}
