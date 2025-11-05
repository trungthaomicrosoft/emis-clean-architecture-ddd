using EMIS.BuildingBlocks.MultiTenant;
using Student.Domain.Enums;
using Student.Domain.ValueObjects;

namespace Student.Domain.Entities;

/// <summary>
/// Entity: Phụ huynh
/// </summary>
public class Parent : TenantEntity
{
    public string FullName { get; private set; }
    public Gender Gender { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public RelationType Relation { get; private set; }
    public ContactInfo ContactInfo { get; private set; }
    public Address? Address { get; private set; }
    public string? Job { get; private set; }
    public string? Workplace { get; private set; }
    public bool IsPrimaryContact { get; private set; }

    // Navigation
    public Guid StudentId { get; private set; }
    public Aggregates.Student Student { get; private set; } = null!;

    private Parent() { } // For EF Core

    public Parent(
        Guid tenantId,
        Guid studentId,
        string fullName,
        Gender gender,
        RelationType relation,
        ContactInfo contactInfo,
        bool isPrimaryContact = false) : base(tenantId)
    {
        StudentId = studentId;
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Gender = gender;
        Relation = relation;
        ContactInfo = contactInfo ?? throw new ArgumentNullException(nameof(contactInfo));
        IsPrimaryContact = isPrimaryContact;
    }

    public void UpdateInfo(
        string fullName,
        Gender gender,
        DateTime? dateOfBirth,
        ContactInfo contactInfo,
        Address? address,
        string? job,
        string? workplace)
    {
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Gender = gender;
        DateOfBirth = dateOfBirth;
        ContactInfo = contactInfo ?? throw new ArgumentNullException(nameof(contactInfo));
        Address = address;
        Job = job;
        Workplace = workplace;
    }

    public void SetPrimaryContact(bool isPrimary)
    {
        IsPrimaryContact = isPrimary;
    }
}
