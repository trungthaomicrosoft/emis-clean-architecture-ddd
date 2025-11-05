using EMIS.BuildingBlocks.ApiResponse;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries;

/// <summary>
/// Query: Lấy thông tin user theo Id
/// </summary>
public class GetUserByIdQuery : IRequest<ApiResponse<UserDto>>
{
    public Guid UserId { get; set; }

    public GetUserByIdQuery(Guid userId)
    {
        UserId = userId;
    }
}
