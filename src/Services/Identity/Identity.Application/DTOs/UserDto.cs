using Identity.Domain.Enums;

namespace Identity.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string FullName { get; set; } = null!;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public Guid? EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
