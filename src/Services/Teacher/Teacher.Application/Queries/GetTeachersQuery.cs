using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Teacher.Application.DTOs;

namespace Teacher.Application.Queries;

/// <summary>
/// Query: Lấy danh sách giáo viên có phân trang
/// </summary>
public class GetTeachersQuery : IRequest<ApiResponse<PagedResult<TeacherDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public int? Status { get; set; }
}
