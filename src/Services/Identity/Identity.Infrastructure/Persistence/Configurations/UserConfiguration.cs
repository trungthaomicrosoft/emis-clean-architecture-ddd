using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(u => u.TenantId)
            .HasColumnName("TenantId")
            .IsRequired();

        // PhoneNumber as Value Object - stored as string
        builder.OwnsOne(u => u.PhoneNumber, phoneNumber =>
        {
            phoneNumber.Property(p => p.Value)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(20)
                .IsRequired();
            
            // Create composite unique index on TenantId and PhoneNumber
            phoneNumber.HasIndex(p => new { p.Value })
                .HasDatabaseName("idx_tenant_phone")
                .IsUnique();
        });

        builder.Property(u => u.Email)
            .HasColumnName("Email")
            .HasMaxLength(255);

        builder.Property(u => u.PasswordHash)
            .HasColumnName("PasswordHash")
            .HasMaxLength(255);

        builder.Property(u => u.FullName)
            .HasColumnName("FullName")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("Role")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.Status)
            .HasColumnName("Status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.EntityId)
            .HasColumnName("EntityId");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("UpdatedAt");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("LastLoginAt");

        builder.Property(u => u.PasswordSetAt)
            .HasColumnName("PasswordSetAt");

        // Relationships
        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("idx_tenant");

        builder.HasIndex(u => u.EntityId)
            .HasDatabaseName("idx_entity");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("idx_status");

        // Ignore domain events collection
        builder.Ignore(u => u.DomainEvents);
    }
}
