using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Identity.Application.Commands;

/// <summary>
/// Command: Thiết lập mật khẩu lần đầu
/// Dành cho Teacher/Parent được admin tạo
/// </summary>
public class SetPasswordCommand : IRequest<ApiResponse<bool>>
{
    public string PhoneNumber { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
