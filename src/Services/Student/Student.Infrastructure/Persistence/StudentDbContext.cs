using EMIS.BuildingBlocks.MultiTenant;
using Microsoft.EntityFrameworkCore;
using Student.Domain.Aggregates;
using Student.Domain.Entities;

namespace Student.Infrastructure.Persistence;

/// <summary>
/// DbContext for Student Service
/// Implements multi-tenancy with query filters
/// </summary>
public class StudentDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public DbSet<Domain.Aggregates.Student> Students => Set<Domain.Aggregates.Student>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<Class> Classes => Set<Class>();

    public StudentDbContext(DbContextOptions<StudentDbContext> options) : base(options)
    {
    }

    public StudentDbContext(
        DbContextOptions<StudentDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentDbContext).Assembly);

        // Global query filter for multi-tenancy
        if (_tenantContext != null)
        {
            modelBuilder.Entity<Domain.Aggregates.Student>()
                .HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);

            modelBuilder.Entity<Parent>()
                .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);

            modelBuilder.Entity<Class>()
                .HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set TenantId for new entities
        if (_tenantContext != null)
        {
            foreach (var entry in ChangeTracker.Entries<EMIS.BuildingBlocks.MultiTenant.TenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.GetType()
                        .GetProperty(nameof(EMIS.BuildingBlocks.MultiTenant.TenantEntity.TenantId))?
                        .SetValue(entry.Entity, _tenantContext.TenantId);
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
