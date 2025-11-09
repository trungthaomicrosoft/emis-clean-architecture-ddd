using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Identity.Application.DTOs;

namespace Identity.Application.Commands;

/// <summary>
/// Command: Đăng ký tenant mới (trường học mới) + tạo School Admin
/// Public endpoint - không cần authentication
/// </summary>
public class RegisterTenantCommand : IRequest<ApiResponse<TenantRegistrationDto>>
{
    /// <summary>
    /// Tên trường học (VD: "Trường Mầm Non Hoa Hồng")
    /// </summary>
    public string SchoolName { get; set; } = null!;

    /// <summary>
    /// Subdomain cho tenant (VD: "truong-hoa-hong" -> truong-hoa-hong.emis.com)
    /// Chỉ chứa chữ thường, số, dấu gạch ngang
    /// </summary>
    public string Subdomain { get; set; } = null!;

    /// <summary>
    /// Email liên hệ của trường
    /// </summary>
    public string ContactEmail { get; set; } = null!;

    /// <summary>
    /// Số điện thoại liên hệ của trường
    /// </summary>
    public string ContactPhone { get; set; } = null!;

    /// <summary>
    /// Địa chỉ trường (optional)
    /// </summary>
    public string? Address { get; set; }

    // Thông tin School Admin (người quản trị chính)

    /// <summary>
    /// Họ tên admin
    /// </summary>
    public string AdminFullName { get; set; } = null!;

    /// <summary>
    /// Số điện thoại admin (dùng để đăng nhập)
    /// </summary>
    public string AdminPhoneNumber { get; set; } = null!;

    /// <summary>
    /// Email admin (optional)
    /// </summary>
    public string? AdminEmail { get; set; }

    /// <summary>
    /// Mật khẩu admin (phải đủ mạnh)
    /// </summary>
    public string AdminPassword { get; set; } = null!;
}
