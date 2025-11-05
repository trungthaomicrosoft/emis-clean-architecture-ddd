using EMIS.SharedKernel;
using Teacher.Domain.Enums;

namespace Teacher.Domain.Events;

/// <summary>
/// Domain Event: Giáo viên được phân công vào lớp
/// </summary>
public class TeacherAssignedToClassEvent : DomainEvent
{
    public Guid TeacherId { get; }
    public Guid ClassId { get; }
    public string ClassName { get; }
    public ClassAssignmentRole Role { get; }
    public Guid TenantId { get; }

    public TeacherAssignedToClassEvent(
        Guid teacherId,
        Guid classId,
        string className,
        ClassAssignmentRole role,
        Guid tenantId)
    {
        TeacherId = teacherId;
        ClassId = classId;
        ClassName = className;
        Role = role;
        TenantId = tenantId;
    }
}
