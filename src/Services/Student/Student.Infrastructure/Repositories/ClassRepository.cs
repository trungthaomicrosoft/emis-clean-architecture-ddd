using Microsoft.EntityFrameworkCore;
using Student.Domain.Entities;
using Student.Domain.Repositories;
using Student.Infrastructure.Persistence;

namespace Student.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Class entity
/// Note: This is read-only reference data from Class Service
/// </summary>
public class ClassRepository : IClassRepository
{
    private readonly StudentDbContext _context;

    public ClassRepository(StudentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Class?> GetByNameAsync(string className, CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .FirstOrDefaultAsync(c => c.ClassName == className, cancellationToken);
    }

    public async Task<IEnumerable<Class>> GetActiveClassesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .Where(c => c.IsActive)
            .OrderBy(c => c.ClassName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsClassFullAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        var classEntity = await _context.Classes
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);

        if (classEntity == null || !classEntity.Capacity.HasValue)
            return false;

        return classEntity.Students.Count >= classEntity.Capacity.Value;
    }

    public async Task<IEnumerable<Class>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .OrderBy(c => c.ClassName)
            .ToListAsync(cancellationToken);
    }
}
