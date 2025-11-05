using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Student.Domain.Entities;

namespace Student.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Class entity
/// Note: Class is managed by Class Service, this is reference data
/// </summary>
public class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> builder)
    {
        builder.ToTable("Classes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever(); // Id comes from Class Service

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.ClassName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Grade)
            .HasMaxLength(50);

        builder.Property(c => c.Capacity);

        builder.Property(c => c.MainTeacherId);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("IX_Classes_TenantId");

        builder.HasIndex(c => new { c.TenantId, c.ClassName })
            .HasDatabaseName("IX_Classes_TenantId_ClassName")
            .IsUnique();

        builder.HasIndex(c => new { c.TenantId, c.IsActive })
            .HasDatabaseName("IX_Classes_TenantId_IsActive");
    }
}
