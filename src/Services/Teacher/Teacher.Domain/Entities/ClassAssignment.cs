using EMIS.BuildingBlocks.MultiTenant;
using Teacher.Domain.Enums;

namespace Teacher.Domain.Entities;

/// <summary>
/// Entity: Phân công giáo viên vào lớp
/// Business Rules:
/// - Một lớp chỉ có 1 giáo viên chủ nhiệm (Primary) tại một thời điểm
/// - Không thể phân công giáo viên không active
/// - EndDate phải sau StartDate
/// </summary>
public class ClassAssignment : TenantEntity
{
    public Guid TeacherId { get; private set; }
    public Guid ClassId { get; private set; }
    public string ClassName { get; private set; } = string.Empty;
    public ClassAssignmentRole Role { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsActive { get; private set; }

    private ClassAssignment() { } // For EF Core

    public ClassAssignment(
        Guid tenantId,
        Guid teacherId,
        Guid classId,
        string className,
        ClassAssignmentRole role,
        DateTime startDate)
        : base(tenantId)
    {
        if (teacherId == Guid.Empty)
            throw new ArgumentException("TeacherId cannot be empty", nameof(teacherId));
        
        if (classId == Guid.Empty)
            throw new ArgumentException("ClassId cannot be empty", nameof(classId));
        
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("ClassName cannot be empty", nameof(className));

        TeacherId = teacherId;
        ClassId = classId;
        ClassName = className;
        Role = role;
        StartDate = startDate;
        IsActive = true;
    }

    public void End(DateTime endDate)
    {
        if (endDate < StartDate)
            throw new ArgumentException("EndDate must be after StartDate", nameof(endDate));

        EndDate = endDate;
        IsActive = false;
    }

    public void UpdateClassName(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("ClassName cannot be empty", nameof(className));

        ClassName = className;
    }

    public void ChangeRole(ClassAssignmentRole newRole)
    {
        Role = newRole;
    }
}
