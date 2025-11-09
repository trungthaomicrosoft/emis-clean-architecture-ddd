using Identity.Domain.Aggregates;

namespace Identity.Domain.Repositories;

/// <summary>
/// Repository interface for Tenant aggregate
/// Following DDD best practices: NO IQueryable exposure
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    /// Thêm tenant mới
    /// </summary>
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tenant theo ID
    /// </summary>
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tenant theo subdomain
    /// </summary>
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra subdomain đã tồn tại chưa
    /// </summary>
    Task<bool> ExistsSubdomainAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách tenant với phân trang
    /// </summary>
    Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
}
