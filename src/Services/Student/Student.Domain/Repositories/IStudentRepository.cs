using EMIS.SharedKernel;
using Student.Domain.ValueObjects;

namespace Student.Domain.Repositories;

/// <summary>
/// Repository interface for Student aggregate
/// </summary>
public interface IStudentRepository : IRepository<Aggregates.Student>
{
    Task<Aggregates.Student> AddAsync(Aggregates.Student entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Aggregates.Student entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Aggregates.Student entity, CancellationToken cancellationToken = default);
    Task<Aggregates.Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Aggregates.Student?> GetByCodeAsync(StudentCode code, CancellationToken cancellationToken = default);
    Task<Aggregates.Student?> GetByIdWithParentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Aggregates.Student?> GetByIdWithClassAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Aggregates.Student>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Aggregates.Student>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> IsCodeExistsAsync(StudentCode code, CancellationToken cancellationToken = default);
    Task<int> GetNextSequenceNumberAsync(int year, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get paginated students with filters (encapsulated query logic)
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Search by name or student code</param>
    /// <param name="status">Filter by student status</param>
    /// <param name="classId">Filter by class</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (items, totalCount)</returns>
    Task<(IEnumerable<Aggregates.Student> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        Enums.StudentStatus? status = null,
        Guid? classId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get paginated students who have parents (with minimum count filter)
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="minParentCount">Minimum number of parents</param>
    /// <param name="searchTerm">Search by name or student code</param>
    /// <param name="status">Filter by student status</param>
    /// <param name="classId">Filter by class</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (items, totalCount)</returns>
    Task<(IEnumerable<Aggregates.Student> Items, int TotalCount)> GetStudentsWithParentsPagedAsync(
        int pageNumber,
        int pageSize,
        int minParentCount = 1,
        string? searchTerm = null,
        Enums.StudentStatus? status = null,
        Guid? classId = null,
        CancellationToken cancellationToken = default);
}
