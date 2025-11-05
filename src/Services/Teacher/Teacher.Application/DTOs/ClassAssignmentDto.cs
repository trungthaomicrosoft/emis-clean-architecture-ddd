namespace Teacher.Application.DTOs;

/// <summary>
/// DTO: Phân công lớp học
/// </summary>
public class ClassAssignmentDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int Role { get; set; } // 1=Primary, 2=Support, 3=Substitute
    public string RoleName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}
