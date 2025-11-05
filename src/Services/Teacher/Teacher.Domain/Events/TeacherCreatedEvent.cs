using EMIS.SharedKernel;

namespace Teacher.Domain.Events;

/// <summary>
/// Domain Event: Giáo viên được tạo
/// </summary>
public class TeacherCreatedEvent : DomainEvent
{
    public Guid TeacherId { get; }
    public string FullName { get; }
    public string PhoneNumber { get; }
    public Guid TenantId { get; }

    public TeacherCreatedEvent(Guid teacherId, string fullName, string phoneNumber, Guid tenantId)
    {
        TeacherId = teacherId;
        FullName = fullName;
        PhoneNumber = phoneNumber;
        TenantId = tenantId;
    }
}
