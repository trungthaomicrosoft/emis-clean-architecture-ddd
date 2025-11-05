using EMIS.SharedKernel;

namespace Teacher.Domain.Events;

/// <summary>
/// Domain Event: Giáo viên được gỡ phân công khỏi lớp
/// </summary>
public class TeacherUnassignedFromClassEvent : DomainEvent
{
    public Guid TeacherId { get; }
    public Guid ClassId { get; }
    public Guid TenantId { get; }

    public TeacherUnassignedFromClassEvent(Guid teacherId, Guid classId, Guid tenantId)
    {
        TeacherId = teacherId;
        ClassId = classId;
        TenantId = tenantId;
    }
}
