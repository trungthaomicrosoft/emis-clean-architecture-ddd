using EMIS.SharedKernel;
using Teacher.Domain.Enums;

namespace Teacher.Domain.Repositories;

/// <summary>
/// Repository interface cho Teacher Aggregate
/// CRITICAL: NO IQueryable exposure - follows DDD best practices
/// All query logic encapsulated in specific methods
/// </summary>
public interface ITeacherRepository : IRepository<Aggregates.Teacher>
{
    /// <summary>
    /// Lấy giáo viên theo Id
    /// </summary>
    Task<Aggregates.Teacher?> GetByIdAsync(Guid teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy giáo viên theo UserId
    /// </summary>
    Task<Aggregates.Teacher?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy giáo viên theo số điện thoại (trong tenant)
    /// </summary>
    Task<Aggregates.Teacher?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách giáo viên có phân trang và bộ lọc
    /// </summary>
    Task<(IEnumerable<Aggregates.Teacher> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TeacherStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy giáo viên theo lớp (bao gồm class assignments)
    /// </summary>
    Task<IEnumerable<Aggregates.Teacher>> GetTeachersByClassAsync(
        Guid classId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy giáo viên kèm class assignments
    /// </summary>
    Task<Aggregates.Teacher?> GetByIdWithAssignmentsAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra số điện thoại đã tồn tại chưa (trong tenant)
    /// </summary>
    Task<bool> ExistsPhoneNumberAsync(
        string phoneNumber,
        Guid? excludeTeacherId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm số lượng giáo viên theo trạng thái
    /// </summary>
    Task<int> CountByStatusAsync(
        TeacherStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy giáo viên chủ nhiệm của lớp
    /// </summary>
    Task<Aggregates.Teacher?> GetPrimaryTeacherByClassAsync(
        Guid classId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm giáo viên mới
    /// </summary>
    Task AddAsync(Aggregates.Teacher teacher, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật giáo viên
    /// </summary>
    void Update(Aggregates.Teacher teacher);

    /// <summary>
    /// Xóa giáo viên
    /// </summary>
    void Delete(Aggregates.Teacher teacher);
}
