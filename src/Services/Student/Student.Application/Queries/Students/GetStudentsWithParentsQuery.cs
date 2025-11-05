using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Student.Application.DTOs;

namespace Student.Application.Queries.Students;

/// <summary>
/// Query để lấy danh sách học sinh đã có phụ huynh
/// </summary>
public class GetStudentsWithParentsQuery : IRequest<ApiResponse<PagedResult<StudentDto>>>
{
    /// <summary>
    /// Số trang (bắt đầu từ 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Số lượng bản ghi trên mỗi trang
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Tìm kiếm theo tên học sinh
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Lọc theo trạng thái học sinh
    /// </summary>
    public Domain.Enums.StudentStatus? Status { get; set; }

    /// <summary>
    /// Lọc theo lớp học
    /// </summary>
    public Guid? ClassId { get; set; }

    /// <summary>
    /// Số lượng phụ huynh tối thiểu (mặc định >= 1)
    /// </summary>
    public int MinParentCount { get; set; } = 1;
}
