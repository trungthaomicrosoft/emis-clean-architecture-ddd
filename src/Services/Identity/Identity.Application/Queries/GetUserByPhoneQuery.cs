using EMIS.BuildingBlocks.ApiResponse;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries;

/// <summary>
/// Query: Lấy thông tin user theo số điện thoại
/// </summary>
public class GetUserByPhoneQuery : IRequest<ApiResponse<UserDto>>
{
    public string PhoneNumber { get; set; } = null!;
}
