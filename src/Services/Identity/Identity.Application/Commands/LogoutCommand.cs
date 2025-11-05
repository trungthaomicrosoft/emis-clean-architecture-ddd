using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Identity.Application.Commands;

/// <summary>
/// Command: Đăng xuất (revoke refresh token)
/// </summary>
public class LogoutCommand : IRequest<ApiResponse<bool>>
{
    public Guid UserId { get; set; }
}
