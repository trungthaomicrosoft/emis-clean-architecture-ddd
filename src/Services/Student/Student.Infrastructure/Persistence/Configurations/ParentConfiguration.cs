using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Student.Domain.Entities;
using Student.Domain.ValueObjects;

namespace Student.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Parent entity
/// </summary>
public class ParentConfiguration : IEntityTypeConfiguration<Parent>
{
    public void Configure(EntityTypeBuilder<Parent> builder)
    {
        builder.ToTable("Parents");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.StudentId)
            .IsRequired();

        builder.Property(p => p.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Gender)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.DateOfBirth);

        builder.Property(p => p.Relation)
            .IsRequired()
            .HasConversion<int>();

        // Value Object: ContactInfo
        builder.OwnsOne(p => p.ContactInfo, contact =>
        {
            contact.Property(c => c.PhoneNumber)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(15)
                .IsRequired();

            contact.Property(c => c.Email)
                .HasColumnName("Email")
                .HasMaxLength(100);

            contact.WithOwner();
        });

        // Value Object: Address
        builder.OwnsOne(p => p.Address, address =>
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

        builder.Property(p => p.Job)
            .HasMaxLength(100);

        builder.Property(p => p.Workplace)
            .HasMaxLength(200);

        builder.Property(p => p.IsPrimaryContact)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_Parents_TenantId");

        builder.HasIndex(p => p.StudentId)
            .HasDatabaseName("IX_Parents_StudentId");

        builder.HasIndex(p => new { p.TenantId, p.StudentId })
            .HasDatabaseName("IX_Parents_TenantId_StudentId");
    }
}
