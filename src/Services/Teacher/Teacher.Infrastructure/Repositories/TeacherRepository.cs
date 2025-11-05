using Microsoft.EntityFrameworkCore;
using Teacher.Domain.Enums;
using Teacher.Domain.Repositories;
using Teacher.Infrastructure.Persistence;

namespace Teacher.Infrastructure.Repositories;

/// <summary>
/// Repository implementation cho Teacher Aggregate
/// CRITICAL: All query logic encapsulated - NO IQueryable exposure!
/// </summary>
public class TeacherRepository : ITeacherRepository
{
    private readonly TeacherDbContext _context;

    public TeacherRepository(TeacherDbContext context)
    {
        _context = context;
    }

    public EMIS.SharedKernel.IUnitOfWork UnitOfWork => _context;

    public async Task<Domain.Aggregates.Teacher?> GetByIdAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Teachers
            .FirstOrDefaultAsync(t => t.Id == teacherId, cancellationToken);
    }

    public async Task<Domain.Aggregates.Teacher?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);
    }

    public async Task<Domain.Aggregates.Teacher?> GetByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.Teachers
            .FirstOrDefaultAsync(t => t.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<(IEnumerable<Domain.Aggregates.Teacher> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TeacherStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        // Start with base query
        var query = _context.Teachers.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(t =>
                t.FullName.ToLower().Contains(searchLower) ||
                t.PhoneNumber.Contains(searchLower) ||
                (t.Email != null && t.Email.ToLower().Contains(searchLower)));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .OrderBy(t => t.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Domain.Aggregates.Teacher>> GetTeachersByClassAsync(
        Guid classId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Teachers
            .Include(t => t.ClassAssignments)
            .Where(t => t.ClassAssignments.Any(ca => ca.ClassId == classId));

        if (activeOnly)
        {
            query = query.Where(t => t.ClassAssignments.Any(ca => ca.ClassId == classId && ca.IsActive));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Domain.Aggregates.Teacher?> GetByIdWithAssignmentsAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Teachers
            .Include(t => t.ClassAssignments)
            .FirstOrDefaultAsync(t => t.Id == teacherId, cancellationToken);
    }

    public async Task<bool> ExistsPhoneNumberAsync(
        string phoneNumber,
        Guid? excludeTeacherId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Teachers.Where(t => t.PhoneNumber == phoneNumber);

        if (excludeTeacherId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTeacherId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> CountByStatusAsync(
        TeacherStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Teachers
            .CountAsync(t => t.Status == status, cancellationToken);
    }

    public async Task<Domain.Aggregates.Teacher?> GetPrimaryTeacherByClassAsync(
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Teachers
            .Include(t => t.ClassAssignments)
            .Where(t => t.ClassAssignments.Any(ca =>
                ca.ClassId == classId &&
                ca.IsActive &&
                ca.Role == ClassAssignmentRole.Primary))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(
        Domain.Aggregates.Teacher teacher,
        CancellationToken cancellationToken = default)
    {
        await _context.Teachers.AddAsync(teacher, cancellationToken);
    }

    public void Update(Domain.Aggregates.Teacher teacher)
    {
        _context.Teachers.Update(teacher);
    }

    public void Delete(Domain.Aggregates.Teacher teacher)
    {
        _context.Teachers.Remove(teacher);
    }
}
