using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Teacher.Application.DTOs;

namespace Teacher.Application.Queries;

/// <summary>
/// Query: Lấy thông tin chi tiết giáo viên theo Id
/// </summary>
public class GetTeacherByIdQuery : IRequest<ApiResponse<TeacherDetailDto>>
{
    public Guid TeacherId { get; set; }

    public GetTeacherByIdQuery(Guid teacherId)
    {
        TeacherId = teacherId;
    }
}
