using Identity.Domain.Aggregates;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Tenant aggregate
/// NOTE: Tenant table does NOT have TenantId column (it's system-level)
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .HasColumnName("Name")
            .HasMaxLength(255)
            .IsRequired();

        // Subdomain as Value Object
        builder.OwnsOne(t => t.Subdomain, subdomain =>
        {
            subdomain.Property(s => s.Value)
                .HasColumnName("Subdomain")
                .HasMaxLength(50)
                .IsRequired();

            // Unique index on subdomain
            subdomain.HasIndex(s => s.Value)
                .HasDatabaseName("idx_subdomain")
                .IsUnique();
        });

        builder.Property(t => t.Status)
            .HasColumnName("Status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.SubscriptionPlan)
            .HasColumnName("SubscriptionPlan")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.SubscriptionExpiresAt)
            .HasColumnName("SubscriptionExpiresAt");

        builder.Property(t => t.MaxUsers)
            .HasColumnName("MaxUsers")
            .IsRequired();

        builder.Property(t => t.ConnectionString)
            .HasColumnName("ConnectionString")
            .HasMaxLength(1000);

        builder.Property(t => t.ContactEmail)
            .HasColumnName("ContactEmail")
            .HasMaxLength(255);

        builder.Property(t => t.ContactPhone)
            .HasColumnName("ContactPhone")
            .HasMaxLength(20);

        builder.Property(t => t.Address)
            .HasColumnName("Address")
            .HasMaxLength(500);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("UpdatedAt");

        // Indexes
        builder.HasIndex(t => t.Status)
            .HasDatabaseName("idx_tenant_status");

        builder.HasIndex(t => t.SubscriptionExpiresAt)
            .HasDatabaseName("idx_subscription_expiry");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("idx_tenant_created");

        // Ignore domain events collection
        builder.Ignore(t => t.DomainEvents);
    }
}
