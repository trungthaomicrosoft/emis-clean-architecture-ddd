using EMIS.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Teacher.Domain.Aggregates;
using Teacher.Domain.Entities;

namespace Teacher.Infrastructure.Persistence;

public class TeacherDbContext : DbContext, IUnitOfWork
{
    private readonly Guid _currentTenantId; // TODO: Inject from ITenantContext

    public TeacherDbContext(DbContextOptions<TeacherDbContext> options)
        : base(options)
    {
        // TODO: Get from ITenantContext
        _currentTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    public DbSet<Domain.Aggregates.Teacher> Teachers { get; set; } = null!;
    public DbSet<ClassAssignment> ClassAssignments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeacherDbContext).Assembly);

        // Global query filter for multi-tenancy
        modelBuilder.Entity<Domain.Aggregates.Teacher>()
            .HasQueryFilter(t => t.TenantId == _currentTenantId);

        modelBuilder.Entity<ClassAssignment>()
            .HasQueryFilter(ca => ca.TenantId == _currentTenantId);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }
}
