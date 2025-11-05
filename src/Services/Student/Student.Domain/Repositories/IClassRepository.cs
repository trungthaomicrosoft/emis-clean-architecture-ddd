using Student.Domain.Entities;

namespace Student.Domain.Repositories;

/// <summary>
/// Repository interface for Class entity
/// Note: Class is managed by Class Service, this is just a reference
/// </summary>
public interface IClassRepository
{
    Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Class?> GetByNameAsync(string className, CancellationToken cancellationToken = default);
    Task<IEnumerable<Class>> GetActiveClassesAsync(CancellationToken cancellationToken = default);
    Task<bool> IsClassFullAsync(Guid classId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Class>> GetAllAsync(CancellationToken cancellationToken = default);
}
