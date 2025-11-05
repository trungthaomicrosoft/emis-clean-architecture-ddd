using EMIS.BuildingBlocks.ApiResponse;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Commands;

/// <summary>
/// Command: Đăng nhập
/// </summary>
public class LoginCommand : IRequest<ApiResponse<AuthResponseDto>>
{
    public string PhoneNumber { get; set; } = null!;
    public string Password { get; set; } = null!;
}
