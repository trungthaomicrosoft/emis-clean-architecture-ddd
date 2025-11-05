using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Student.Application.DTOs;

namespace Student.Application.Queries.Students;

/// <summary>
/// Query to get students by class
/// </summary>
public class GetStudentsByClassQuery : IRequest<ApiResponse<List<StudentDto>>>
{
    public Guid ClassId { get; set; }

    public GetStudentsByClassQuery(Guid classId)
    {
        ClassId = classId;
    }
}
