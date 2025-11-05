using EMIS.SharedKernel;

namespace Teacher.Domain.Events;

/// <summary>
/// Domain Event: Giáo viên được cập nhật
/// </summary>
public class TeacherUpdatedEvent : DomainEvent
{
    public Guid TeacherId { get; }
    public string FullName { get; }
    public Guid TenantId { get; }

    public TeacherUpdatedEvent(Guid teacherId, string fullName, Guid tenantId)
    {
        TeacherId = teacherId;
        FullName = fullName;
        TenantId = tenantId;
    }
}
