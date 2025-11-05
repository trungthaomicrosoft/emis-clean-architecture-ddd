using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Student.Application.DTOs;

namespace Student.Application.Queries.Students;

/// <summary>
/// Query to get student by code
/// </summary>
public class GetStudentByCodeQuery : IRequest<ApiResponse<StudentDetailDto>>
{
    public string StudentCode { get; set; }

    public GetStudentByCodeQuery(string studentCode)
    {
        StudentCode = studentCode;
    }
}
