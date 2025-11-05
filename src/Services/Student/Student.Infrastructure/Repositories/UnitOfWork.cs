using EMIS.SharedKernel;
using Student.Infrastructure.Persistence;

namespace Student.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation using EF Core
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly StudentDbContext _context;

    public UnitOfWork(StudentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await DispatchDomainEventsAsync(cancellationToken);

        // Save changes to database
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await SaveChangesAsync(cancellationToken);
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<EMIS.SharedKernel.Entity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents!)
            .ToList();

        domainEntities.ForEach(entity => entity.ClearDomainEvents());

        // TODO: Publish domain events using MediatR or EventBus
        // For now, just clear them
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
