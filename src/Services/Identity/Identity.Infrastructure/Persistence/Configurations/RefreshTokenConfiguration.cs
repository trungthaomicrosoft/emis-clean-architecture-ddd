using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(rt => rt.TenantId)
            .HasColumnName("TenantId")
            .IsRequired();

        builder.Property(rt => rt.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(rt => rt.Token)
            .HasColumnName("Token")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("ExpiresAt")
            .IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("RevokedAt");

        builder.Property(rt => rt.IsRevoked)
            .HasColumnName("IsRevoked")
            .IsRequired();

        // Indexes
        builder.HasIndex(rt => rt.Token)
            .HasDatabaseName("idx_token");

        builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.ExpiresAt })
            .HasDatabaseName("idx_user_active");

        // Ignore domain events
        builder.Ignore(rt => rt.DomainEvents);
    }
}
