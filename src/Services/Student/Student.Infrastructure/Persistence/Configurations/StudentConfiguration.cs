using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Student.Domain.ValueObjects;

namespace Student.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Student aggregate root
/// </summary>
public class StudentConfiguration : IEntityTypeConfiguration<Domain.Aggregates.Student>
{
    public void Configure(EntityTypeBuilder<Domain.Aggregates.Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever(); // We manage Guid generation

        builder.Property(s => s.TenantId)
            .IsRequired();

        // Value Object: StudentCode
        builder.OwnsOne(s => s.StudentCode, code =>
        {
            code.Property(c => c.Value)
                .HasColumnName("StudentCode")
                .HasMaxLength(12)
                .IsRequired();

            code.WithOwner();
        });

        builder.HasIndex(s => new { s.TenantId, s.StudentCode })
            .HasDatabaseName("IX_Students_TenantId_StudentCode")
            .IsUnique();

        builder.Property(s => s.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Gender)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.DateOfBirth)
            .IsRequired();

        builder.Property(s => s.PlaceOfBirth)
            .HasMaxLength(200);

        builder.Property(s => s.Nationality)
            .HasMaxLength(100);

        builder.Property(s => s.EthnicGroup)
            .HasMaxLength(100);

        // Value Object: Address
        builder.OwnsOne(s => s.Address, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("AddressStreet")
                .HasMaxLength(200);

            address.Property(a => a.Ward)
                .HasColumnName("AddressWard")
                .HasMaxLength(100);

            address.Property(a => a.District)
                .HasColumnName("AddressDistrict")
                .HasMaxLength(100);

            address.Property(a => a.City)
                .HasColumnName("AddressCity")
                .HasMaxLength(100);

            address.Property(a => a.PostalCode)
                .HasColumnName("AddressPostalCode")
                .HasMaxLength(20);

            address.WithOwner();
        });

        builder.Property(s => s.Avatar)
            .HasMaxLength(500);

        builder.Property(s => s.HealthNotes)
            .HasMaxLength(1000);

        builder.Property(s => s.Allergies)
            .HasMaxLength(500);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.EnrollmentDate)
            .IsRequired();

        builder.Property(s => s.GraduationDate);

        // Foreign Key to Class
        builder.Property(s => s.ClassId);

        builder.HasOne(s => s.Class)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.SetNull);

        // One-to-Many: Student -> Parents
        builder.HasMany(s => s.Parents)
            .WithOne(p => p.Student)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events (not stored in database)
        builder.Ignore(s => s.DomainEvents);

        // Indexes
        builder.HasIndex(s => s.TenantId)
            .HasDatabaseName("IX_Students_TenantId");

        builder.HasIndex(s => s.ClassId)
            .HasDatabaseName("IX_Students_ClassId");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_Students_Status");

        builder.HasIndex(s => new { s.TenantId, s.Status })
            .HasDatabaseName("IX_Students_TenantId_Status");
    }
}
