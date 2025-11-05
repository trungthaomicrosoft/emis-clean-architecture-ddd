using EMIS.SharedKernel;

namespace Student.Domain.Events;

/// <summary>
/// Domain Event: Thông tin học sinh được cập nhật
/// </summary>
public class StudentUpdatedEvent : DomainEvent
{
    public Guid StudentId { get; }
    public string FullName { get; }
    public Guid TenantId { get; }

    public StudentUpdatedEvent(Guid studentId, string fullName, Guid tenantId)
    {
        StudentId = studentId;
        FullName = fullName;
        TenantId = tenantId;
    }
}
