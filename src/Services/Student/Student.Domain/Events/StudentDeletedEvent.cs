using EMIS.SharedKernel;

namespace Student.Domain.Events;

/// <summary>
/// Domain Event: Học sinh bị xóa
/// </summary>
public class StudentDeletedEvent : DomainEvent
{
    public Guid StudentId { get; }
    public string StudentCode { get; }
    public Guid TenantId { get; }

    public StudentDeletedEvent(Guid studentId, string studentCode, Guid tenantId)
    {
        StudentId = studentId;
        StudentCode = studentCode;
        TenantId = tenantId;
    }
}
