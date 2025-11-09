using EMIS.SharedKernel;
using Identity.Domain.Aggregates;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext, IUnitOfWork
{
    private readonly Guid _currentTenantId; // TODO: Inject from ITenantContext

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
        // TODO: Get from ITenantContext
        _currentTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!; // System-level, NO tenant filter;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Global query filter for multi-tenancy
        // NOTE: Tenants table does NOT have this filter - it's system-level
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == _currentTenantId);

        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(rt => rt.TenantId == _currentTenantId);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }
}
