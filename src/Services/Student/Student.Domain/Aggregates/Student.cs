using EMIS.BuildingBlocks.Exceptions;
using EMIS.BuildingBlocks.MultiTenant;
using EMIS.SharedKernel;
using Student.Domain.Entities;
using Student.Domain.Enums;
using Student.Domain.Events;
using Student.Domain.ValueObjects;

namespace Student.Domain.Aggregates;

/// <summary>
/// Aggregate Root: Học sinh
/// Business Rules:
/// - Mã học sinh phải unique trong tenant
/// - Ít nhất 1 phụ huynh là primary contact
/// - Tuổi phải từ 0-6 tuổi (mầm non)
/// - Chỉ có thể assign vào lớp active
/// </summary>
public class Student : TenantEntity, IAggregateRoot
{
    private readonly List<Parent> _parents = new();

    public StudentCode StudentCode { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public Gender Gender { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public string? PlaceOfBirth { get; private set; }
    public string? Nationality { get; private set; }
    public string? EthnicGroup { get; private set; }
    public Address? Address { get; private set; }
    public string? Avatar { get; private set; }
    public string? HealthNotes { get; private set; }
    public string? Allergies { get; private set; }
    public StudentStatus Status { get; private set; }
    public DateTime EnrollmentDate { get; private set; }
    public DateTime? GraduationDate { get; private set; }

    // Navigation
    public Guid? ClassId { get; private set; }
    public Class? Class { get; private set; }

    public IReadOnlyCollection<Parent> Parents => _parents.AsReadOnly();

    private Student() { } // For EF Core

    public Student(
        Guid tenantId,
        StudentCode studentCode,
        string fullName,
        Gender gender,
        DateTime dateOfBirth,
        DateTime enrollmentDate)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        ValidateAge(dateOfBirth);

        TenantId = tenantId;
        StudentCode = studentCode ?? throw new ArgumentNullException(nameof(studentCode));
        FullName = fullName;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        EnrollmentDate = enrollmentDate;
        Status = StudentStatus.Active;

        // Add domain event
        base.AddDomainEvent(new StudentCreatedEvent(Id, studentCode, fullName, dateOfBirth, tenantId));
    }

    public void UpdateInfo(
        string fullName,
        Gender gender,
        DateTime dateOfBirth,
        string? placeOfBirth,
        string? nationality,
        string? ethnicGroup,
        Address? address)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        ValidateAge(dateOfBirth);

        FullName = fullName;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        PlaceOfBirth = placeOfBirth;
        Nationality = nationality;
        EthnicGroup = ethnicGroup;
        Address = address;

        base.AddDomainEvent(new StudentUpdatedEvent(Id, fullName, TenantId));
    }

    public void UpdateHealthInfo(string? healthNotes, string? allergies)
    {
        HealthNotes = healthNotes;
        Allergies = allergies;
    }

    public void SetAvatar(string avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
            throw new ArgumentException("Avatar URL cannot be empty", nameof(avatarUrl));

        Avatar = avatarUrl;
    }

    public void AssignToClass(Guid classId, Class classEntity)
    {
        if (classEntity == null)
            throw new ArgumentNullException(nameof(classEntity));

        if (!classEntity.IsActive)
            throw new BusinessRuleValidationException("Cannot assign student to inactive class");

        if (classEntity.Capacity.HasValue && classEntity.Students.Count >= classEntity.Capacity.Value)
            throw new BusinessRuleValidationException($"Class {classEntity.ClassName} is full (capacity: {classEntity.Capacity})");

        ClassId = classId;
        Class = classEntity;
    }

    public void RemoveFromClass()
    {
        ClassId = null;
        Class = null;
    }

    public void AddParent(Parent parent)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        if (parent.TenantId != TenantId)
            throw new BusinessRuleValidationException("Parent must belong to the same tenant");

        _parents.Add(parent);
    }

    public void RemoveParent(Guid parentId)
    {
        var parent = _parents.FirstOrDefault(p => p.Id == parentId);
        if (parent == null)
            throw new NotFoundException($"Parent with id {parentId} not found");

        if (parent.IsPrimaryContact && _parents.Count(p => p.IsPrimaryContact) == 1)
            throw new BusinessRuleValidationException("Cannot remove the only primary contact");

        _parents.Remove(parent);
    }

    public void SetPrimaryContact(Guid parentId)
    {
        var parent = _parents.FirstOrDefault(p => p.Id == parentId);
        if (parent == null)
            throw new NotFoundException($"Parent with id {parentId} not found");

        // Remove primary from others
        foreach (var p in _parents.Where(p => p.IsPrimaryContact))
        {
            p.SetPrimaryContact(false);
        }

        // Set new primary
        parent.SetPrimaryContact(true);
    }

    public void ChangeStatus(StudentStatus newStatus)
    {
        if (Status == newStatus)
            return;

        // Business rules for status transitions
        if (Status == StudentStatus.Graduated && newStatus != StudentStatus.Graduated)
            throw new BusinessRuleValidationException("Cannot change status of graduated student");

        if (newStatus == StudentStatus.Graduated)
        {
            GraduationDate = DateTime.UtcNow;
        }

        Status = newStatus;
    }

    public void Graduate()
    {
        ChangeStatus(StudentStatus.Graduated);
    }

    public void Suspend()
    {
        ChangeStatus(StudentStatus.Inactive);
    }

    public void Reactivate()
    {
        ChangeStatus(StudentStatus.Active);
    }

    private void ValidateAge(DateTime dateOfBirth)
    {
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (dateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;

        if (age < 0 || age > 6)
            throw new BusinessRuleValidationException($"Student age must be between 0 and 6 years (current: {age})");
    }

    public int GetAge()
    {
        var age = DateTime.UtcNow.Year - DateOfBirth.Year;
        if (DateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
        return age;
    }

    public Parent? GetPrimaryContact()
    {
        return _parents.FirstOrDefault(p => p.IsPrimaryContact);
    }
}
