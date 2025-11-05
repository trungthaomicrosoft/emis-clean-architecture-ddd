using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teacher.Domain.Entities;

namespace Teacher.Infrastructure.Persistence.Configurations;

public class ClassAssignmentConfiguration : IEntityTypeConfiguration<ClassAssignment>
{
    public void Configure(EntityTypeBuilder<ClassAssignment> builder)
    {
        builder.ToTable("ClassAssignments");

        builder.HasKey(ca => ca.Id);

        builder.Property(ca => ca.Id)
            .ValueGeneratedNever();

        builder.Property(ca => ca.TeacherId)
            .IsRequired();

        builder.Property(ca => ca.ClassId)
            .IsRequired();

        builder.Property(ca => ca.ClassName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ca => ca.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ca => ca.StartDate)
            .IsRequired();

        builder.Property(ca => ca.EndDate);

        builder.Property(ca => ca.IsActive)
            .IsRequired();

        builder.Property(ca => ca.TenantId)
            .IsRequired();

        // Indexes
        builder.HasIndex(ca => ca.TenantId);
        builder.HasIndex(ca => ca.TeacherId);
        builder.HasIndex(ca => ca.ClassId);
        builder.HasIndex(ca => new { ca.ClassId, ca.IsActive });
        builder.HasIndex(ca => new { ca.TeacherId, ca.IsActive });

        // Ignore domain events
        builder.Ignore(ca => ca.DomainEvents);
    }
}
