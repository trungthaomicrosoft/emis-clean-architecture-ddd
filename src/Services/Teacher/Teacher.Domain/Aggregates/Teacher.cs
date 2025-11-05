using EMIS.BuildingBlocks.Exceptions;
using EMIS.BuildingBlocks.MultiTenant;
using EMIS.SharedKernel;
using Teacher.Domain.Entities;
using Teacher.Domain.Enums;
using Teacher.Domain.Events;
using Teacher.Domain.ValueObjects;

namespace Teacher.Domain.Aggregates;

/// <summary>
/// Aggregate Root: Giáo viên
/// Business Rules:
/// - PhoneNumber phải unique trong tenant (dùng làm username)
/// - Ít nhất 18 tuổi
/// - Không thể xóa giáo viên đang có phân công active
/// - Chỉ có thể phân công vào lớp khi status = Active
/// - Một lớp chỉ có 1 giáo viên chủ nhiệm (Primary) tại một thời điểm
/// </summary>
public class Teacher : TenantEntity, IAggregateRoot
{
    private readonly List<ClassAssignment> _classAssignments = new();

    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = null!;
    public DateTime? DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string PhoneNumber { get; private set; } = null!;
    public string? Email { get; private set; }
    public Address? Address { get; private set; }
    public string? Avatar { get; private set; }
    public DateTime? HireDate { get; private set; }
    public TeacherStatus Status { get; private set; }

    public IReadOnlyCollection<ClassAssignment> ClassAssignments => _classAssignments.AsReadOnly();

    private Teacher() { } // For EF Core

    public Teacher(
        Guid tenantId,
        Guid userId,
        string fullName,
        Gender gender,
        string phoneNumber,
        DateTime? hireDate = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        TenantId = tenantId;
        UserId = userId;
        FullName = fullName;
        Gender = gender;
        PhoneNumber = phoneNumber;
        HireDate = hireDate ?? DateTime.UtcNow;
        Status = TeacherStatus.Active;

        // Add domain event
        base.AddDomainEvent(new TeacherCreatedEvent(Id, fullName, phoneNumber, tenantId));
    }

    public void UpdateInfo(
        string fullName,
        Gender gender,
        DateTime? dateOfBirth,
        string? email,
        Address? address)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        if (dateOfBirth.HasValue)
        {
            ValidateAge(dateOfBirth.Value);
        }

        FullName = fullName;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Email = email;
        Address = address;

        base.AddDomainEvent(new TeacherUpdatedEvent(Id, fullName, TenantId));
    }

    public void UpdatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        PhoneNumber = phoneNumber;
    }

    public void SetAvatar(string avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
            throw new ArgumentException("Avatar URL cannot be empty", nameof(avatarUrl));

        Avatar = avatarUrl;
    }

    public void AssignToClass(Guid classId, string className, ClassAssignmentRole role, DateTime? startDate = null)
    {
        if (Status != TeacherStatus.Active)
            throw new BusinessRuleValidationException("Cannot assign inactive teacher to class");

        if (classId == Guid.Empty)
            throw new ArgumentException("ClassId cannot be empty", nameof(classId));

        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("ClassName cannot be empty", nameof(className));

        // Check if already assigned to this class with active assignment
        var existingAssignment = _classAssignments
            .FirstOrDefault(a => a.ClassId == classId && a.IsActive);

        if (existingAssignment != null)
            throw new BusinessRuleValidationException($"Teacher is already assigned to class {className}");

        var assignment = new ClassAssignment(
            TenantId,
            Id,
            classId,
            className,
            role,
            startDate ?? DateTime.UtcNow);

        _classAssignments.Add(assignment);

        base.AddDomainEvent(new TeacherAssignedToClassEvent(Id, classId, className, role, TenantId));
    }

    public void UnassignFromClass(Guid classId, DateTime? endDate = null)
    {
        var assignment = _classAssignments
            .FirstOrDefault(a => a.ClassId == classId && a.IsActive);

        if (assignment == null)
            throw new NotFoundException($"No active assignment found for class {classId}");

        assignment.End(endDate ?? DateTime.UtcNow);

        base.AddDomainEvent(new TeacherUnassignedFromClassEvent(Id, classId, TenantId));
    }

    public void ChangeStatus(TeacherStatus newStatus)
    {
        if (Status == newStatus)
            return;

        // Business rules for status transitions
        if (Status == TeacherStatus.Resigned && newStatus != TeacherStatus.Resigned)
            throw new BusinessRuleValidationException("Cannot change status of resigned teacher");

        if (Status == TeacherStatus.Terminated && newStatus != TeacherStatus.Terminated)
            throw new BusinessRuleValidationException("Cannot change status of terminated teacher");

        // Check active assignments when changing to inactive status
        if (newStatus != TeacherStatus.Active && HasActiveAssignments())
            throw new BusinessRuleValidationException(
                "Cannot change teacher status to inactive while having active class assignments. " +
                "Please unassign from all classes first.");

        Status = newStatus;

        base.AddDomainEvent(new TeacherStatusChangedEvent(Id, Status, TenantId));
    }

    public void Resign()
    {
        ChangeStatus(TeacherStatus.Resigned);
    }

    public void Terminate()
    {
        ChangeStatus(TeacherStatus.Terminated);
    }

    public void Reactivate()
    {
        if (Status == TeacherStatus.Resigned || Status == TeacherStatus.Terminated)
            throw new BusinessRuleValidationException(
                "Cannot reactivate resigned or terminated teacher. Please create a new teacher record.");

        ChangeStatus(TeacherStatus.Active);
    }

    private void ValidateAge(DateTime dateOfBirth)
    {
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (dateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;

        if (age < 18)
            throw new BusinessRuleValidationException($"Teacher must be at least 18 years old (current: {age})");
    }

    public int? GetAge()
    {
        if (!DateOfBirth.HasValue)
            return null;

        var age = DateTime.UtcNow.Year - DateOfBirth.Value.Year;
        if (DateOfBirth.Value > DateTime.UtcNow.AddYears(-age)) age--;
        return age;
    }

    public bool HasActiveAssignments()
    {
        return _classAssignments.Any(a => a.IsActive);
    }

    public IEnumerable<ClassAssignment> GetActiveAssignments()
    {
        return _classAssignments.Where(a => a.IsActive);
    }

    public ClassAssignment? GetPrimaryClassAssignment()
    {
        return _classAssignments
            .FirstOrDefault(a => a.IsActive && a.Role == ClassAssignmentRole.Primary);
    }
}
