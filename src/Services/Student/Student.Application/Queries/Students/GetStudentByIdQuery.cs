using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Student.Application.DTOs;

namespace Student.Application.Queries.Students;

/// <summary>
/// Query to get student by ID
/// </summary>
public class GetStudentByIdQuery : IRequest<ApiResponse<StudentDetailDto>>
{
    public Guid Id { get; set; }

    public GetStudentByIdQuery(Guid id)
    {
        Id = id;
    }
}
