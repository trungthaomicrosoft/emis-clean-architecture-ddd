using EMIS.SharedKernel;
using Teacher.Domain.Enums;

namespace Teacher.Domain.Events;

/// <summary>
/// Domain Event: Trạng thái giáo viên thay đổi
/// </summary>
public class TeacherStatusChangedEvent : DomainEvent
{
    public Guid TeacherId { get; }
    public TeacherStatus NewStatus { get; }
    public Guid TenantId { get; }

    public TeacherStatusChangedEvent(Guid teacherId, TeacherStatus newStatus, Guid tenantId)
    {
        TeacherId = teacherId;
        NewStatus = newStatus;
        TenantId = tenantId;
    }
}
