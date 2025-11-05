using EMIS.BuildingBlocks.MultiTenant;

namespace Student.Domain.Entities;

/// <summary>
/// Entity: Lớp học
/// Đây là reference entity - thông tin chi tiết quản lý ở Class Service
/// </summary>
public class Class : TenantEntity
{
    public string ClassName { get; private set; }
    public string? Grade { get; private set; }
    public int? Capacity { get; private set; }
    public Guid? MainTeacherId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    private readonly List<Aggregates.Student> _students = new();
    public IReadOnlyCollection<Aggregates.Student> Students => _students.AsReadOnly();

    private Class() { } // For EF Core

    public Class(Guid id, Guid tenantId, string className) : base(tenantId)
    {
        Id = id;
        ClassName = className ?? throw new ArgumentNullException(nameof(className));
        IsActive = true;
    }

    public void UpdateInfo(string className, string? grade, int? capacity, Guid? mainTeacherId, string? description)
    {
        ClassName = className ?? throw new ArgumentNullException(nameof(className));
        Grade = grade;
        Capacity = capacity;
        MainTeacherId = mainTeacherId;
        Description = description;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
