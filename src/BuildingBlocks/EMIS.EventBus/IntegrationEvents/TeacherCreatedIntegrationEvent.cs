namespace EMIS.EventBus.IntegrationEvents;

/// <summary>
/// Integration event published when a new teacher is created
/// </summary>
public class TeacherCreatedIntegrationEvent : IntegrationEvent
{
    public Guid TeacherId { get; set; }
    public Guid TenantId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }

    public TeacherCreatedIntegrationEvent(
        Guid teacherId,
        Guid tenantId,
        string phoneNumber,
        string fullName,
        string? email)
    {
        TeacherId = teacherId;
        TenantId = tenantId;
        PhoneNumber = phoneNumber;
        FullName = fullName;
        Email = email;
    }

    // For deserialization
    public TeacherCreatedIntegrationEvent() { }
}
