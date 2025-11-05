using EMIS.SharedKernel;
using Identity.Infrastructure.Persistence;

namespace Identity.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;

    public UnitOfWork(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveEntitiesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
