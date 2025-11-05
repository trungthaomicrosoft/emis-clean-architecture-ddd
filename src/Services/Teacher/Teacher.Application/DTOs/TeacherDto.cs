namespace Teacher.Application.DTOs;

/// <summary>
/// DTO: Giáo viên (thông tin cơ bản)
/// </summary>
public class TeacherDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string GenderName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
