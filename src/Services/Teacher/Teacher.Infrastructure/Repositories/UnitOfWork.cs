using EMIS.SharedKernel;
using Teacher.Infrastructure.Persistence;

namespace Teacher.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation using EF Core
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TeacherDbContext _context;

    public UnitOfWork(TeacherDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Publish domain events here
        var result = await _context.SaveChangesAsync(cancellationToken);
        return result > 0;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
