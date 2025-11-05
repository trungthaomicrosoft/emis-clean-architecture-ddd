using EMIS.BuildingBlocks.ApiResponse;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands;

/// <summary>
/// Command: Đăng ký School Admin (có password ngay)
/// </summary>
public class RegisterAdminCommand : IRequest<ApiResponse<Guid>>
{
    public string PhoneNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Email { get; set; }
}
