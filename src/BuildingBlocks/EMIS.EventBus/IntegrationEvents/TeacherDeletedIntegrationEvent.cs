namespace EMIS.EventBus.IntegrationEvents;

/// <summary>
/// Integration event published when a teacher is deleted
/// </summary>
public class TeacherDeletedIntegrationEvent : IntegrationEvent
{
    public Guid TeacherId { get; set; }
    public Guid TenantId { get; set; }

    public TeacherDeletedIntegrationEvent(Guid teacherId, Guid tenantId)
    {
        TeacherId = teacherId;
        TenantId = tenantId;
    }

    // For deserialization
    public TeacherDeletedIntegrationEvent() { }
}
