using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Student.Application.DTOs;

namespace Student.Application.Queries.Students;

/// <summary>
/// Query to get paginated list of students
/// </summary>
public class GetStudentsQuery : IRequest<ApiResponse<PagedResult<StudentDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public Guid? ClassId { get; set; }
    public int? Status { get; set; }
}
