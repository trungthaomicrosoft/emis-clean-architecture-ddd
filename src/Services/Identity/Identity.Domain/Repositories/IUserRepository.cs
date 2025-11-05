using EMIS.SharedKernel;
using Identity.Domain.Aggregates;
using Identity.Domain.Enums;

namespace Identity.Domain.Repositories;

/// <summary>
/// Repository interface cho User Aggregate
/// CRITICAL: NO IQueryable exposure - all queries encapsulated
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Lấy user theo Id
    /// </summary>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy user theo số điện thoại (username)
    /// </summary>
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy user theo EntityId (TeacherId hoặc ParentId từ service khác)
    /// </summary>
    Task<User?> GetByEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra số điện thoại đã tồn tại chưa
    /// </summary>
    Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách user có phân trang
    /// </summary>
    Task<(IEnumerable<User> Users, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        UserRole? role = null,
        UserStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy user với refresh tokens
    /// </summary>
    Task<User?> GetByIdWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy user theo refresh token
    /// </summary>
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thêm user mới
    /// </summary>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật user
    /// </summary>
    void Update(User user);

    /// <summary>
    /// Xóa user
    /// </summary>
    void Delete(User user);
}
