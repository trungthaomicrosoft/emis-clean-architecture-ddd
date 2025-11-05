using EMIS.SharedKernel;

namespace Student.Domain.Events;

/// <summary>
/// Domain Event: Học sinh được tạo mới
/// </summary>
public class StudentCreatedEvent : DomainEvent
{
    public Guid StudentId { get; }
    public string StudentCode { get; }
    public string FullName { get; }
    public DateTime DateOfBirth { get; }
    public Guid TenantId { get; }

    public StudentCreatedEvent(
        Guid studentId, 
        string studentCode, 
        string fullName, 
        DateTime dateOfBirth,
        Guid tenantId)
    {
        StudentId = studentId;
        StudentCode = studentCode;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        TenantId = tenantId;
    }
}
