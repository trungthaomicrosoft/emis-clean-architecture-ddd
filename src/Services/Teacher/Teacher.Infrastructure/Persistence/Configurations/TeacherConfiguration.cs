using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Teacher.Infrastructure.Persistence.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Domain.Aggregates.Teacher>
{
    public void Configure(EntityTypeBuilder<Domain.Aggregates.Teacher> builder)
    {
        builder.ToTable("Teachers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Email)
            .HasMaxLength(255);

        builder.Property(t => t.Avatar)
            .HasMaxLength(1000);

        builder.Property(t => t.Gender)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.DateOfBirth);

        builder.Property(t => t.HireDate);

        builder.Property(t => t.TenantId)
            .IsRequired();

        // Address value object
        builder.OwnsOne(t => t.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(255);
            address.Property(a => a.Ward).HasColumnName("Ward").HasMaxLength(100);
            address.Property(a => a.District).HasColumnName("District").HasMaxLength(100);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
        });

        // Class assignments - One-to-Many relationship
        builder.HasMany(t => t.ClassAssignments)
            .WithOne()
            .HasForeignKey(ca => ca.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => new { t.TenantId, t.PhoneNumber }).IsUnique();
        builder.HasIndex(t => t.Status);

        // Ignore domain events
        builder.Ignore(t => t.DomainEvents);
    }
}
