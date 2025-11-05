using EMIS.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Student.Domain.Aggregates;
using Student.Domain.Repositories;
using Student.Domain.ValueObjects;
using Student.Infrastructure.Persistence;

namespace Student.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Student aggregate
/// </summary>
public class StudentRepository : IStudentRepository
{
    private readonly StudentDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public IUnitOfWork UnitOfWork => _unitOfWork;

    public StudentRepository(StudentDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Domain.Aggregates.Student> AddAsync(Domain.Aggregates.Student entity, CancellationToken cancellationToken = default)
    {
        await _context.Students.AddAsync(entity, cancellationToken);
        return entity;
    }

    public Task UpdateAsync(Domain.Aggregates.Student entity, CancellationToken cancellationToken = default)
    {
        _context.Students.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Domain.Aggregates.Student entity, CancellationToken cancellationToken = default)
    {
        _context.Students.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<Domain.Aggregates.Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.Student?> GetByCodeAsync(StudentCode code, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.StudentCode == code, cancellationToken);
    }

    public async Task<Domain.Aggregates.Student?> GetByIdWithParentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .Include(s => s.Parents)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Domain.Aggregates.Student?> GetByIdWithClassAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Domain.Aggregates.Student>> GetByClassIdAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCodeExistsAsync(StudentCode code, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .AnyAsync(s => s.StudentCode == code, cancellationToken);
    }

    public async Task<int> GetNextSequenceNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        // Get the highest sequence number for this year
        var prefix = $"HS{year}";
        
        var lastStudent = await _context.Students
            .Where(s => EF.Functions.Like(s.StudentCode.ToString(), $"{prefix}%"))
            .OrderByDescending(s => s.StudentCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastStudent == null)
            return 1;

        // Extract sequence from code (e.g., HS2025000001 -> 000001)
        var codeValue = lastStudent.StudentCode.Value;
        var sequenceStr = codeValue.Substring(6); // Skip "HS2025"
        
        if (int.TryParse(sequenceStr, out int sequence))
            return sequence + 1;

        return 1;
    }

    public async Task<IEnumerable<Domain.Aggregates.Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .OrderBy(s => s.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Domain.Aggregates.Student> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        Domain.Enums.StudentStatus? status = null,
        Guid? classId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Students.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(s =>
                s.FullName.ToLower().Contains(searchLower) ||
                s.StudentCode.ToString().ToLower().Contains(searchLower));
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        // Apply class filter
        if (classId.HasValue)
        {
            query = query.Where(s => s.ClassId == classId.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .OrderBy(s => s.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Domain.Aggregates.Student> Items, int TotalCount)> GetStudentsWithParentsPagedAsync(
        int pageNumber,
        int pageSize,
        int minParentCount = 1,
        string? searchTerm = null,
        Domain.Enums.StudentStatus? status = null,
        Guid? classId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Students.AsQueryable();

        // Filter students with minimum parent count
        query = query.Where(s => s.Parents.Count >= minParentCount);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(s =>
                s.FullName.ToLower().Contains(searchLower) ||
                s.StudentCode.ToString().ToLower().Contains(searchLower));
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        // Apply class filter
        if (classId.HasValue)
        {
            query = query.Where(s => s.ClassId == classId.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .OrderByDescending(s => s.EnrollmentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
