namespace EMIS.EventBus.IntegrationEvents;

/// <summary>
/// Integration event published when a new student is created
/// Chat Service subscribes to this event to automatically create a student group conversation
/// </summary>
public class StudentCreatedIntegrationEvent : IntegrationEvent
{
    public Guid StudentId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClassId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<ParentInfo> Parents { get; set; } = new();
    public Guid CreatedBy { get; set; }

    public StudentCreatedIntegrationEvent(
        Guid studentId,
        Guid tenantId,
        Guid classId,
        string studentName,
        List<ParentInfo> parents,
        Guid createdBy)
    {
        StudentId = studentId;
        TenantId = tenantId;
        ClassId = classId;
        StudentName = studentName;
        Parents = parents;
        CreatedBy = createdBy;
    }

    // For deserialization
    public StudentCreatedIntegrationEvent() { }
}

/// <summary>
/// Parent information included in the student created event
/// </summary>
public class ParentInfo
{
    public Guid ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty; // Father, Mother, Guardian

    public ParentInfo() { }

    public ParentInfo(Guid parentId, string parentName, string relationship)
    {
        ParentId = parentId;
        ParentName = parentName;
        Relationship = relationship;
    }
}
