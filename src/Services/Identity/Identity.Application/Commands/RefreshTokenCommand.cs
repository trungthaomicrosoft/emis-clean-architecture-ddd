using EMIS.BuildingBlocks.ApiResponse;
using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Commands;

/// <summary>
/// Command: Refresh access token
/// </summary>
public class RefreshTokenCommand : IRequest<ApiResponse<AuthResponseDto>>
{
    public string RefreshToken { get; set; } = null!;
}
