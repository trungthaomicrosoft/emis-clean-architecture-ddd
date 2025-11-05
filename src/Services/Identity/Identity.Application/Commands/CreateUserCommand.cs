using EMIS.BuildingBlocks.ApiResponse;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands;

/// <summary>
/// Command: Tạo user mới (Teacher/Parent) - chưa có password
/// Admin thêm vào, user sẽ set password lần đầu
/// </summary>
public class CreateUserCommand : IRequest<ApiResponse<Guid>>
{
    public string PhoneNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public UserRole Role { get; set; }
    public Guid? EntityId { get; set; }
    public string? Email { get; set; }
}
